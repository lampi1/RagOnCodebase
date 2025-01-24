using System.Text;
using System.Text.Json;
using Elasticsearch.Net;
using Nest;
using System.Text.RegularExpressions;
using CodebaseAI.Models;

public class ChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ElasticClient _elasticClient;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILocalizationService _localizationService;
    private readonly string _azureEmbeddingEndpoint;
    private readonly string _azureCompletionEndpoint;
    private readonly string _elasticApiKey;
    private readonly string _elasticCloudID;
    private readonly string _elasticCloudEndpoint;

    public ChatService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration, 
        IChatHistoryService chatHistoryService,
        ILocalizationService localizationService)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _chatHistoryService = chatHistoryService ?? throw new ArgumentNullException(nameof(chatHistoryService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        
        _azureEmbeddingEndpoint = configuration["AzureOpenAI:EmbeddingEndpoint"];
        _azureCompletionEndpoint = configuration["AzureOpenAI:CompletionEndpoint"];

        _elasticApiKey = configuration["ElasticSearch:ApiKey"];
        _elasticCloudID = configuration["ElasticSearch:CloudId"];
        _elasticCloudEndpoint = configuration["ElasticSearch:CloudEndPoint"];

        var cloudSettings = new ConnectionSettings(new Uri(_elasticCloudEndpoint))
            .DefaultIndex("codebase_index_v2")
            .ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(_elasticApiKey));

        _elasticClient = new ElasticClient(cloudSettings);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var requestBody = new { input = text };
            var jsonString = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("AzureOpenAI");
            var response = await client.PostAsync(_azureEmbeddingEndpoint, content);
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
            Console.WriteLine(_localizationService.GetString("HttpRequestError", ex.Message));
            throw;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(_localizationService.GetString("JsonDeserializationError", ex.Message));
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(_localizationService.GetString("GeneralError", ex.Message));
            throw;
        }
    }
public async Task<PaginatedResult<string>> SearchDocumentsWithEmbeddingAsync(
    string queryText, 
    int pageSize = 10, 
    int pageNumber = 1)
{
    try
    {
        if (pageSize < 1) pageSize = 10;
        if (pageNumber < 1) pageNumber = 1;

        var queryEmbedding = await GenerateEmbeddingAsync(queryText);

        var searchResponse = await _elasticClient.SearchAsync<dynamic>(s => s
            .Query(q => q
                .ScriptScore(ss => ss
                    .Query(qq => qq.MatchAll())
                    .Script(script => script
                        .Source("cosineSimilarity(params.query_vector, 'embedding') + 1.0")
                        .Params(p => p.Add("query_vector", queryEmbedding))
                    )
                )
            )
            .From((pageNumber - 1) * pageSize)
            .Size(pageSize)
            .TrackTotalHits()
        );

        if (!searchResponse.IsValid)
        {
            Console.WriteLine(_localizationService.GetString("SearchError", searchResponse.DebugInformation));
            return new PaginatedResult<string>(Array.Empty<string>(), pageNumber, pageSize, 0);
        }

        var documents = new List<string>();
        foreach (var hit in searchResponse.Hits)
        {
            hit.Source.TryGetValue("content", out object contentValue);
            hit.Source.TryGetValue("file_name", out object fileNameValue);
            hit.Source.TryGetValue("path", out object pathValue);

            string content = contentValue?.ToString() ?? "N/A";
            string fileName = fileNameValue?.ToString() ?? "N/A";
            string path = pathValue?.ToString() ?? "N/A";

            string document = $"Content: {content}, File Name: {fileName}, Path: {path}";
            documents.Add(document);
        }

        return new PaginatedResult<string>(
            documents, 
            pageNumber, 
            pageSize, 
            (int)searchResponse.Total
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(_localizationService.GetString("SearchError", ex.Message));
        return new PaginatedResult<string>(Array.Empty<string>(), pageNumber, pageSize, 0);
    }
}

    public async Task<string> GetResponseAsync(string userInput)
    {
        try
        {
            _chatHistoryService.AddMessage(new ChatMessageContent("user", userInput));

            var searchResults = await SearchDocumentsWithEmbeddingAsync(userInput, pageSize: 5, pageNumber: 1);
            var finalContent = $"CONTENUTO DEL PROGETTO CHE DEVI ANALIZZARE ATTENTAMENTE PER RISPONDERE ALL' UTENTE:";
            foreach (var result in searchResults.Items)
            {
                finalContent += $"{result}";
            }

            _chatHistoryService.AddMessage(new ChatMessageContent("system", $"{finalContent}"));

            const int maxHistoryMessages = 4;
            _chatHistoryService.TrimHistory(maxHistoryMessages);

            var messages = _chatHistoryService.GetHistory().Select(message => new
            {
                role = message.Role,
                content = message.Content
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
            var client = _httpClientFactory.CreateClient("AzureOpenAI");
            var response = await client.PostAsync(azureChatEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString);
            var choices = (JsonElement)jsonResponse["choices"];
            var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();

            _chatHistoryService.AddMessage(new ChatMessageContent("assistant", messageContent));
            return messageContent;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(_localizationService.GetString("HttpRequestError", ex.Message));
            return _localizationService.GetString("CompletionServiceError");
        }
        catch (JsonException ex)
        {
            Console.WriteLine(_localizationService.GetString("JsonDeserializationError", ex.Message));
            return _localizationService.GetString("CompletionResponseError");
        }
        catch (Exception ex)
        {
            Console.WriteLine(_localizationService.GetString("GeneralError", ex.Message));
            return _localizationService.GetString("GeneralCompletionError");
        }
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
