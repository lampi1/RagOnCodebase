using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using CodebaseAI.Models;

namespace CodebaseAI.Tests
{
    public class ChatServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IChatHistoryService> _mockChatHistoryService;
        private readonly Mock<ILocalizationService> _mockLocalizationService;
        private readonly Mock<ILogger<ChatService>> _mockLogger;
        private readonly IOptions<AzureOpenAIOptions> _azureOptions;
        private readonly IOptions<ElasticsearchOptions> _elasticOptions;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockChatHistoryService = new Mock<IChatHistoryService>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockLogger = new Mock<ILogger<ChatService>>();
            
            _azureOptions = Options.Create(new AzureOpenAIOptions 
            { 
                EmbeddingEndpoint = "http://test.com/embedding",
                CompletionEndpoint = "http://test.com/completion"
            });
            
            _elasticOptions = Options.Create(new ElasticsearchOptions
            {
                CloudEndPoint = "http://test.elastic.com",
                DefaultIndex = "test-index",
                ApiKey = "test-key"
            });

            _chatService = new ChatService(
                _mockHttpClientFactory.Object,
                _azureOptions,
                _elasticOptions,
                _mockChatHistoryService.Object,
                _mockLocalizationService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task SearchDocumentsWithEmbeddingAsync_WithNullQuery_ThrowsArgumentNullException()
        {
            // Arrange
            string queryText = null;
            var options = new SearchOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _chatService.SearchDocumentsWithEmbeddingAsync(queryText, options));
        }

        [Fact]
        public async Task SearchDocumentsWithEmbeddingAsync_WithEmptyQuery_ThrowsArgumentNullException()
        {
            // Arrange
            string queryText = "";
            var options = new SearchOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _chatService.SearchDocumentsWithEmbeddingAsync(queryText, options));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task SearchDocumentsWithEmbeddingAsync_WithInvalidPageSize_AdjustsToDefault(int pageSize)
        {
            // Arrange
            string queryText = "test query";
            var options = new SearchOptions { PageSize = pageSize };

            // Act
            options.Validate();

            // Assert
            Assert.Equal(10, options.PageSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task SearchDocumentsWithEmbeddingAsync_WithInvalidPageNumber_AdjustsToDefault(int pageNumber)
        {
            // Arrange
            string queryText = "test query";
            var options = new SearchOptions { PageNumber = pageNumber };

            // Act
            options.Validate();

            // Assert
            Assert.Equal(1, options.PageNumber);
        }

        [Fact]
        public async Task SearchDocumentsWithEmbeddingAsync_WithValidOptions_ValidatesSuccessfully()
        {
            // Arrange
            var options = new SearchOptions 
            { 
                PageSize = 20,
                PageNumber = 2,
                SortBy = SearchSortOption.DateDescending
            };

            // Act
            options.Validate();

            // Assert
            Assert.Equal(20, options.PageSize);
            Assert.Equal(2, options.PageNumber);
            Assert.Equal(SearchSortOption.DateDescending, options.SortBy);
        }

        [Fact]
        public async Task SearchDocumentsWithEmbeddingAsync_WithLargePageSize_AdjustsToMaximum()
        {
            // Arrange
            var options = new SearchOptions { PageSize = 200 };

            // Act
            options.Validate();

            // Assert
            Assert.Equal(100, options.PageSize);
        }
    }
}