using Xunit;
using CodebaseAI.Services;

namespace CodebaseAI.Tests
{
    public class ChatHistoryServiceTests
    {
        private readonly ChatHistoryService _chatHistoryService;

        public ChatHistoryServiceTests()
        {
            _chatHistoryService = new ChatHistoryService();
        }

        [Fact]
        public void AddMessage_AddsMessageToHistory()
        {
            // Arrange
            var message = new ChatMessageContent("user", "test message");

            // Act
            _chatHistoryService.AddMessage(message);
            var history = _chatHistoryService.GetHistory();

            // Assert
            Assert.Single(history);
            Assert.Equal(message, history.First());
        }

        [Fact]
        public void TrimHistory_KeepsFirstSystemMessageAndLimitsOthers()
        {
            // Arrange
            var systemMessage = new ChatMessageContent("system", "system message");
            var userMessage1 = new ChatMessageContent("user", "user message 1");
            var assistantMessage1 = new ChatMessageContent("assistant", "assistant message 1");
            var userMessage2 = new ChatMessageContent("user", "user message 2");
            var assistantMessage2 = new ChatMessageContent("assistant", "assistant message 2");

            _chatHistoryService.AddMessage(systemMessage);
            _chatHistoryService.AddMessage(userMessage1);
            _chatHistoryService.AddMessage(assistantMessage1);
            _chatHistoryService.AddMessage(userMessage2);
            _chatHistoryService.AddMessage(assistantMessage2);

            // Act
            _chatHistoryService.TrimHistory(4); // Keep system message + 3 recent messages
            var history = _chatHistoryService.GetHistory();

            // Assert
            Assert.Equal(4, history.Count);
            Assert.Equal(systemMessage, history[0]);
            Assert.Equal(assistantMessage1, history[1]);
            Assert.Equal(userMessage2, history[2]);
            Assert.Equal(assistantMessage2, history[3]);
        }

        [Fact]
        public void Clear_RemovesAllMessages()
        {
            // Arrange
            _chatHistoryService.AddMessage(new ChatMessageContent("user", "test message"));
            _chatHistoryService.AddMessage(new ChatMessageContent("assistant", "test response"));

            // Act
            _chatHistoryService.Clear();
            var history = _chatHistoryService.GetHistory();

            // Assert
            Assert.Empty(history);
        }

        [Fact]
        public void GetHistory_ReturnsUnmodifiableList()
        {
            // Arrange
            _chatHistoryService.AddMessage(new ChatMessageContent("user", "test message"));
            var history = _chatHistoryService.GetHistory();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => history.Add(new ChatMessageContent("user", "another message")));
        }
    }
}