using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using CodebaseAI.Models;
using CodebaseAI.Services;

namespace CodebaseAI.Tests
{
    public class ChatServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IChatHistoryService> _chatHistoryServiceMock;
        private readonly Mock<ILocalizationService> _localizationServiceMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly Mock<IOptions<AzureOpenAIOptions>> _azureOptionsMock;
        private readonly Mock<IOptions<ElasticsearchOptions>> _elasticOptionsMock;

        public ChatServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _chatHistoryServiceMock = new Mock<IChatHistoryService>();
            _localizationServiceMock = new Mock<ILocalizationService>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _azureOptionsMock = new Mock<IOptions<AzureOpenAIOptions>>();
            _elasticOptionsMock = new Mock<IOptions<ElasticsearchOptions>>();

            // Setup Azure OpenAI options
            _azureOptionsMock.Setup(x => x.Value).Returns(new AzureOpenAIOptions
            {
                ApiKey = "test-key",
                EmbeddingEndpoint = "https://test.openai.azure.com/embeddings",
                CompletionEndpoint = "https://test.openai.azure.com/chat/completions"
            });

            // Setup Elasticsearch options
            _elasticOptionsMock.Setup(x => x.Value).Returns(new ElasticsearchOptions
            {
                ApiKey = "test-elastic-key",
                CloudId = "test-cloud-id",
                CloudEndpoint = "https://test.elastic.cloud",
                DefaultIndex = "codebase_index_v2"
            });

            // Setup HttpClient
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient("AzureOpenAI")).Returns(httpClient);
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_Success()
        {
            // Arrange
            var embeddingResponse = new
            {
                data = new[]
                {
                    new { embedding = new float[] { 0.1f, 0.2f, 0.3f } }
                }
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(embeddingResponse))
                });

            var chatService = new ChatService(
                _httpClientFactoryMock.Object,
                _azureOptionsMock.Object,
                _elasticOptionsMock.Object,
                _chatHistoryServiceMock.Object,
                _localizationServiceMock.Object
            );

            // Act
            var result = await chatService.GenerateEmbeddingAsync("test text");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal(0.1f, result[0]);
            Assert.Equal(0.2f, result[1]);
            Assert.Equal(0.3f, result[2]);
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_HttpError_ThrowsAndLogsError()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Test error"));

            _localizationServiceMock
                .Setup(x => x.GetString("HttpRequestError", It.IsAny<object[]>()))
                .Returns("HTTP request error: Test error");

            var chatService = new ChatService(
                _httpClientFactoryMock.Object,
                _azureOptionsMock.Object,
                _elasticOptionsMock.Object,
                _chatHistoryServiceMock.Object,
                _localizationServiceMock.Object
            );

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => 
                chatService.GenerateEmbeddingAsync("test text"));

            _localizationServiceMock.Verify(
                x => x.GetString("HttpRequestError", It.IsAny<object[]>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchDocumentsWithEmbeddingAsync_ReturnsPagedResults()
        {
            // Arrange
            var chatService = new ChatService(
                _httpClientFactoryMock.Object,
                _azureOptionsMock.Object,
                _elasticOptionsMock.Object,
                _chatHistoryServiceMock.Object,
                _localizationServiceMock.Object
            );

            // Setup embedding response
            var embeddingResponse = new
            {
                data = new[]
                {
                    new { embedding = new float[] { 0.1f, 0.2f, 0.3f } }
                }
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(embeddingResponse))
                });

            // Act
            var result = await chatService.SearchDocumentsWithEmbeddingAsync("test", 5, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(5, result.PageSize);
        }

        [Fact]
        public async Task GetResponseAsync_Success()
        {
            // Arrange
            var chatService = new ChatService(
                _httpClientFactoryMock.Object,
                _azureOptionsMock.Object,
                _elasticOptionsMock.Object,
                _chatHistoryServiceMock.Object,
                _localizationServiceMock.Object
            );

            var completionResponse = new
            {
                choices = new[]
                {
                    new { message = new { content = "Test response" } }
                }
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(completionResponse))
                });

            // Act
            var result = await chatService.GetResponseAsync("test question");

            // Assert
            Assert.Equal("Test response", result);
            _chatHistoryServiceMock.Verify(
                x => x.AddMessage(It.IsAny<ChatMessageContent>()),
                Times.Exactly(3)); // user, system, and assistant messages
        }
    }
}
