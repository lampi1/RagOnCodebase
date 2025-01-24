using CodebaseAI.Models;

namespace CodebaseAI.Services
{
    public interface IChatHistoryService
    {
        void AddMessage(ChatMessageContent message);
        IReadOnlyList<ChatMessageContent> GetHistory();
        void TrimHistory(int maxMessages);
        void Clear();
    }
}
