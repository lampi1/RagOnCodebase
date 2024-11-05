using System.Text;
using System.Text.Json;
using Elasticsearch.Net;
using Nest;
using System.Text.RegularExpressions;

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
            new ChatMessageContent("system", "Devi cercare di rispondere alle domande dell' utente ('user') in modo breve e conciso e basare le tue risposte sul contenuto del progetto che sono pezzi di file con content (contenuto del progetto) filename (nome del file) e path (percorso del file nel progetto). Se l'utente non ti fa domande relative al progetto puoi rispondere in modo generico e dire che tu sei qui per rispondere alle domande del progetto. Sii consapevole che il progetto sono pezzi di file che ti do in base alla domanda dell' utente, se non hai il contenuto necessario significa che l'utente non ti ha fatto domande specifiche.")
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
            .Size(1)
        );

        if (!searchResponse.IsValid)
        {
            Console.WriteLine($"Errore nella ricerca: {searchResponse.DebugInformation}");
            return new List<string>();
        }

        var documents = new List<string>();
        foreach (var hit in searchResponse.Hits)
        {
            // Usa TryGetValue per verificare la presenza dei campi
            hit.Source.TryGetValue("content", out object contentValue);
            hit.Source.TryGetValue("file_name", out object fileNameValue);
            hit.Source.TryGetValue("path", out object pathValue);

            string content = contentValue?.ToString() ?? "N/A";
            string fileName = fileNameValue?.ToString() ?? "N/A";
            string path = pathValue?.ToString() ?? "N/A";

            string document = $"Content: {content}, File Name: {fileName}, Path: {path}";
            documents.Add(document);
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
            var finalContent = $"CONTENUTO DEL PROGETTO CHE DEVI ANALIZZARE ATTENTAMENTE PER RISPONDERE ALL' UTENTE:";
            foreach (var result in searchResults)
            {
                finalContent += $"{result}";
            }

            _chatHistory.Add(new ChatMessageContent("system", $"{finalContent}"));

            const int maxHistoryMessages = 4;

            if (_chatHistory.Count > maxHistoryMessages)
            {
                // Mantieni il primo messaggio di sistema e limita i successivi a un massimo di 3
                var systemMessage = _chatHistory[0];
                var recentMessages = _chatHistory.Skip(_chatHistory.Count - (maxHistoryMessages - 1)).ToList();
                _chatHistory.Clear();
                _chatHistory.Add(systemMessage);
                _chatHistory.AddRange(recentMessages);
            }
            var messages = _chatHistory.Select(message => new
            {
                role = message.Role,
                content = CleanContent(message.Content)
            }).ToList();

            var requestBody = new
            {
                messages = messages,
                max_tokens = 500,
                temperature = 0.5, // Valore per rendere le risposte più variate
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
        // Rimuove i commenti di linea (//) preservando le stringhe
        content = Regex.Replace(content, @"(?<!:)//.*", string.Empty);

        // Rimuove i commenti di blocco (/* */) preservando le stringhe
        content = Regex.Replace(content, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

        // Rimuove linee vuote e spaziature superflue
        content = Regex.Replace(content, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline);

        // Rimuove spaziature all'inizio e alla fine del testo
        content = content.Trim();

        return content;
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
