using System.Text.RegularExpressions;

public class ChatHistoryService : IChatHistoryService
{
    private readonly List<ChatMessageContent> _chatHistory;
    private readonly object _lock = new object();

    public ChatHistoryService()
    {
        _chatHistory = new List<ChatMessageContent>
        {
            new ChatMessageContent("system", "Devi cercare di rispondere alle domande dell' utente ('user') in modo breve e conciso e basare le tue risposte sul contenuto del progetto che sono pezzi di file con content (contenuto del progetto) filename (nome del file) e path (percorso del file nel progetto). Se l'utente non ti fa domande relative al progetto puoi rispondere in modo generico e dire che tu sei qui per rispondere alle domande del progetto. Sii consapevole che il progetto sono pezzi di file che ti do in base alla domanda dell' utente, se non hai il contenuto necessario significa che l'utente non ti ha fatto domande specifiche.")
        };
    }

    public void AddMessage(ChatMessageContent message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        lock (_lock)
        {
            _chatHistory.Add(message);
        }
    }

    public List<ChatMessageContent> GetHistory()
    {
        lock (_lock)
        {
            return _chatHistory.ToList();
        }
    }

    public void TrimHistory(int maxMessages)
    {
        if (maxMessages < 1)
            throw new ArgumentException("maxMessages must be greater than 0", nameof(maxMessages));

        lock (_lock)
        {
            if (_chatHistory.Count > maxMessages)
            {
                // Keep the first system message and limit subsequent messages
                var systemMessage = _chatHistory[0];
                var recentMessages = _chatHistory.Skip(_chatHistory.Count - (maxMessages - 1)).ToList();
                _chatHistory.Clear();
                _chatHistory.Add(systemMessage);
                _chatHistory.AddRange(recentMessages);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            var systemMessage = _chatHistory[0];
            _chatHistory.Clear();
            _chatHistory.Add(systemMessage);
        }
    }

    public string CleanContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Remove line comments (//) preserving strings
        content = Regex.Replace(content, @"(?<!:)//.*", string.Empty);

        // Remove block comments (/* */) preserving strings
        content = Regex.Replace(content, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

        // Remove empty lines and extra whitespace
        content = Regex.Replace(content, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline);

        // Remove whitespace at the beginning and end of the text
        return content.Trim();
    }
}