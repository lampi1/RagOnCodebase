using CodebaseAI.Models;

namespace CodebaseAI.Services
{
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
            lock (_lock)
            {
                _chatHistory.Add(message);
            }
        }

        public IReadOnlyList<ChatMessageContent> GetHistory()
        {
            lock (_lock)
            {
                return _chatHistory.ToList().AsReadOnly();
            }
        }

        public void TrimHistory(int maxMessages)
        {
            if (maxMessages < 1)
                throw new ArgumentException("maxMessages must be greater than 0", nameof(maxMessages));

            lock (_lock)
            {
                if (_chatHistory.Count <= maxMessages)
                    return;

                var systemMessage = _chatHistory[0];
                var recentMessages = _chatHistory.Skip(_chatHistory.Count - (maxMessages - 1)).ToList();
                _chatHistory.Clear();
                _chatHistory.Add(systemMessage);
                _chatHistory.AddRange(recentMessages);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _chatHistory.Clear();
            }
        }
    }
}
