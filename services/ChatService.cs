using System.Text;
using System.Text.Json;
using Elasticsearch.Net;
using Nest;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly ElasticClient _elasticClient;
    private readonly List<ChatMessageContent> _chatHistory;
    private readonly string _azureApiKey;
    private readonly string _azureEmbeddingEndpoint;
    private readonly string _azureCompletionEndpoint;
    private readonly string _elasticApiKey;
    private readonly string _elasticCloudID;
    private readonly string _elasticCloudEndpoint;

    public ChatService(IConfiguration configuration)
    {
        _azureApiKey = configuration["AzureOpenAI:ApiKey"];
        _azureEmbeddingEndpoint = configuration["AzureOpenAI:EmbeddingEndpoint"];
        _azureCompletionEndpoint = configuration["AzureOpenAI:CompletionEndpoint"];

        _elasticApiKey = configuration["ElasticSearch:ApiKey"];
        _elasticCloudID = configuration["ElasticSearch:CloudId"];
        _elasticCloudEndpoint = configuration["ElasticSearch:CloudEndPoint"];
        _httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        _httpClient.DefaultRequestHeaders.Add("api-key", _azureApiKey);

        var cloudSettings = new ConnectionSettings(new Uri(_elasticCloudEndpoint))
            .DefaultIndex("codebase_index_v2")
            .ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(_elasticApiKey));

        _elasticClient = new ElasticClient(cloudSettings);

        _chatHistory = new List<ChatMessageContent>
        {
            new ChatMessageContent("system", "Sei un'assistente virtuale per un team di sviluppo software, progettata per rispondere alle domande tecniche e di progetto degli utenti (USER) in modo preciso e sintetico. Il tuo compito è utilizzare i dettagli forniti nei messaggi di sistema, che contengono estratti di file, documentazione e altre informazioni rilevanti del progetto. Rispondi alle domande basandoti su queste informazioni e mantieni una memoria a breve termine delle risposte e dei dettagli discussi. Se un utente chiede qualcosa di non specificato nei messaggi di sistema, chiedi gentilmente ulteriori dettagli come il nome di un file o di una classe per chiarire meglio la richiesta. Assumi un tono professionale e gentile, supportando gli sviluppatori con informazioni concise, ma non esitare a richiedere dettagli aggiuntivi quando necessario per rispondere con precisione.")
        };
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var requestBody = new { input = text };
            var jsonString = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_azureEmbeddingEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString);

            var dataArray = (JsonElement)jsonResponse["data"];
            var embeddingArray = dataArray[0].GetProperty("embedding");

            var embedding = JsonSerializer.Deserialize<float[]>(embeddingArray.GetRawText());
            return embedding;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Errore nella richiesta HTTP: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Errore nella deserializzazione JSON: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore generico durante la generazione dell'embedding: {ex.Message}");
            throw;
        }
    }
    public async Task<List<string>> SearchDocumentsWithEmbeddingAsync(string queryText)
    {
        try
        {
            var queryEmbedding = await GenerateEmbeddingAsync(queryText);

            var searchResponse = _elasticClient.Search<dynamic>(s => s
                .Query(q => q
                    .ScriptScore(ss => ss
                        .Query(qq => qq.MatchAll())
                        .Script(script => script
                            .Source("cosineSimilarity(params.query_vector, 'embedding') + 1.0")
                            .Params(p => p.Add("query_vector", queryEmbedding))
                        )
                    )
                )
                .Size(2)
            );

            if (!searchResponse.IsValid)
            {
                Console.WriteLine($"Errore nella ricerca: {searchResponse.DebugInformation}");
                return new List<string>();
            }

            var documents = new List<string>();
            foreach (var hit in searchResponse.Hits)
            {
                if (hit.Source.TryGetValue("content", out object contentValue))
                {
                    documents.Add(contentValue.ToString());
                }
            }

            return documents;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante la ricerca dei documenti: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<string> GetResponseAsync(string userInput)
    {
        try
        {
            _chatHistory.Add(new ChatMessageContent("user", userInput));

            var searchResults = await SearchDocumentsWithEmbeddingAsync(userInput);

            foreach (var result in searchResults)
            {
                _chatHistory.Add(new ChatMessageContent("system", $"CONTESTO DI PROGETTO: {result}"));
            }

            const int maxHistoryMessages = 7;
            if (_chatHistory.Count > maxHistoryMessages)
            {
                _chatHistory.RemoveRange(0, _chatHistory.Count - maxHistoryMessages);
            }
            var messages = _chatHistory.Select(message => new
            {
                role = message.Role,
                content = CleanContent(message.Content)
            }).ToList();

            var requestBody = new
            {
                model = "gpt-35-turbo",
                messages = messages,
                max_tokens = 500,
            };

            var jsonString = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            // Invio la richiesta all'IA di Azure
            string azureChatEndpoint = _azureCompletionEndpoint;
            var response = await _httpClient.PostAsync(azureChatEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString);
            var choices = (JsonElement)jsonResponse["choices"];
            var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();

            _chatHistory.Add(new ChatMessageContent("assistant", messageContent));
            return messageContent;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Errore nella richiesta HTTP: {ex.Message}");
            return "Errore durante la richiesta al servizio di completamento.";
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Errore nella deserializzazione JSON: {ex.Message}");
            return "Errore durante l'elaborazione della risposta del servizio di completamento.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore generico durante la generazione della risposta: {ex.Message}");
            return "Si è verificato un errore durante la generazione della risposta.";
        }
    }

    private string CleanContent(string content)
    {
        return content.Replace("\n", " ")
                      .Replace("\r", " ")
                      .Replace("\\", " ")
                      .Replace("\"", "'")
                      .Replace("\t", " ")
                      .Trim();
    }
}

public class ChatMessageContent
{
    public string Role { get; }
    public string Content { get; }

    public ChatMessageContent(string role, string content)
    {
        Role = role;
        Content = content;
    }
}
