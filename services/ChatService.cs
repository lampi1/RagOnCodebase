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
    private readonly ILogger<ChatService> _logger;
    private readonly AzureOpenAIOptions _azureOptions;
    private readonly ElasticsearchOptions _elasticOptions;

    public ChatService(
        IHttpClientFactory httpClientFactory,
        IOptions<AzureOpenAIOptions> azureOptions,
        IOptions<ElasticsearchOptions> elasticOptions,
        IChatHistoryService chatHistoryService,
        ILocalizationService localizationService,
        ILogger<ChatService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _chatHistoryService = chatHistoryService ?? throw new ArgumentNullException(nameof(chatHistoryService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _azureOptions = azureOptions?.Value ?? throw new ArgumentNullException(nameof(azureOptions));
        _elasticOptions = elasticOptions?.Value ?? throw new ArgumentNullException(nameof(elasticOptions));

        _elasticClient = CreateElasticClient(_elasticOptions);
    }

    private static ElasticClient CreateElasticClient(ElasticsearchOptions options)
    {
        var cloudSettings = new ConnectionSettings(new Uri(options.CloudEndPoint))
            .DefaultIndex(options.DefaultIndex)
            .ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(options.ApiKey));

        return new ElasticClient(cloudSettings);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var requestBody = new { input = text };
            var jsonString = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("AzureOpenAI");
            var response = await client.PostAsync(_azureOptions.EmbeddingEndpoint, content);
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
/// <summary>
/// Searches for documents using semantic similarity with the provided query text.
/// </summary>
/// <param name="queryText">The text to search for.</param>
/// <param name="options">The search options including pagination and sorting preferences.</param>
/// <returns>A paginated result containing matching documents.</returns>
/// <exception cref="ArgumentNullException">Thrown when queryText is null or empty.</exception>
/// <exception cref="ArgumentException">Thrown when search options are invalid.</exception>
public async Task<PaginatedResult<string>> SearchDocumentsWithEmbeddingAsync(
    string queryText,
    SearchOptions options = null)
{
    if (string.IsNullOrEmpty(queryText))
    {
        throw new ArgumentNullException(nameof(queryText), "Query text cannot be null or empty.");
    }

    options ??= new SearchOptions();
    options.Validate();

    try
    {
        var queryEmbedding = await GenerateEmbeddingAsync(queryText);

        var searchRequest = new SearchDescriptor<dynamic>()
            .Query(q => q
                .ScriptScore(ss => ss
                    .Query(qq => qq.MatchAll())
                    .Script(script => script
                        .Source("cosineSimilarity(params.query_vector, 'embedding') + 1.0")
                        .Params(p => p.Add("query_vector", queryEmbedding))
                    )
                )
            )
            .From((options.PageNumber - 1) * options.PageSize)
            .Size(options.PageSize)
            .TrackTotalHits();

        // Add sorting based on the selected option
        switch (options.SortBy)
        {
            case SearchSortOption.DateAscending:
                searchRequest = searchRequest.Sort(s => s.Ascending("date"));
                break;
            case SearchSortOption.DateDescending:
                searchRequest = searchRequest.Sort(s => s.Descending("date"));
                break;
            case SearchSortOption.FileName:
                searchRequest = searchRequest.Sort(s => s.Ascending("file_name.keyword"));
                break;
            // For Relevance, we use the default script score sorting
        }

        var searchResponse = await _elasticClient.SearchAsync<dynamic>(searchRequest);

        if (!searchResponse.IsValid)
        {
            _logger.LogError("Search failed: {ErrorMessage}", searchResponse.DebugInformation);
            throw new InvalidOperationException($"Search operation failed: {searchResponse.ServerError?.Error?.Reason}");
        }

        var documents = new List<string>();
        foreach (var hit in searchResponse.Hits)
        {
            hit.Source.TryGetValue("content", out object contentValue);
            hit.Source.TryGetValue("file_name", out object fileNameValue);
            hit.Source.TryGetValue("path", out object pathValue);
            hit.Source.TryGetValue("date", out object dateValue);

            string content = contentValue?.ToString() ?? "N/A";
            string fileName = fileNameValue?.ToString() ?? "N/A";
            string path = pathValue?.ToString() ?? "N/A";
            string date = dateValue?.ToString() ?? "N/A";
            double score = hit.Score ?? 0.0;

            string document = $"Content: {content}\nFile: {fileName}\nPath: {path}\nDate: {date}\nRelevance Score: {score:F2}";
            documents.Add(document);
        }

        return new PaginatedResult<string>(
            documents,
            options.PageNumber,
            options.PageSize,
            (int)searchResponse.Total
        );
    }
    catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
    {
        _logger.LogError(ex, "Error occurred while searching documents with query: {QueryText}", queryText);
        throw new InvalidOperationException("An error occurred while searching documents. Please try again later.", ex);
    }
}

    public async Task<string> GetResponseAsync(string userInput)
    {
        try
        {
            _chatHistoryService.AddMessage(new ChatMessageContent("user", userInput));

            var searchOptions = new SearchOptions { PageSize = 5, PageNumber = 1, SortBy = SearchSortOption.Relevance };
            var searchResults = await SearchDocumentsWithEmbeddingAsync(userInput, searchOptions);
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
            string azureChatEndpoint = _azureOptions.CompletionEndpoint;
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
