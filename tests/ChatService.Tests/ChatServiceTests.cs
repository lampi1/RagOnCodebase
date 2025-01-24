using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IChatHistoryService> _chatHistoryServiceMock;
        private readonly Mock<ILocalizationService> _localizationServiceMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

        public ChatServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();
            _chatHistoryServiceMock = new Mock<IChatHistoryService>();
            _localizationServiceMock = new Mock<ILocalizationService>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Setup configuration
            _configurationMock.Setup(x => x["AzureOpenAI:ApiKey"]).Returns("test-key");
            _configurationMock.Setup(x => x["AzureOpenAI:EmbeddingEndpoint"]).Returns("https://test.openai.azure.com/embeddings");
            _configurationMock.Setup(x => x["AzureOpenAI:CompletionEndpoint"]).Returns("https://test.openai.azure.com/chat/completions");
            _configurationMock.Setup(x => x["ElasticSearch:ApiKey"]).Returns("test-elastic-key");
            _configurationMock.Setup(x => x["ElasticSearch:CloudId"]).Returns("test-cloud-id");
            _configurationMock.Setup(x => x["ElasticSearch:CloudEndPoint"]).Returns("https://test.elastic.cloud");

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
                _configurationMock.Object,
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
                _configurationMock.Object,
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
                _configurationMock.Object,
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
                _configurationMock.Object,
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
