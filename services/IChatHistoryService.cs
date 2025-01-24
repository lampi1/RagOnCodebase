public interface IChatHistoryService
{
    void AddMessage(ChatMessageContent message);
    List<ChatMessageContent> GetHistory();
    void TrimHistory(int maxMessages);
    void Clear();
}