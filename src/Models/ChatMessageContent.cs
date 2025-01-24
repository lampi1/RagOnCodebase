namespace CodebaseAI.Models
{
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
}