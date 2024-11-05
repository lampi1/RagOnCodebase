using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ChatModel : PageModel
{
    private readonly ChatService _chatService;

    public ChatModel(ChatService chatService)
    {
        _chatService = chatService;
    }

    [BindProperty]
    public string UserInput { get; set; }

    public List<(string Role, string Content)> ChatHistory { get; set; } = new();

    public async Task OnPostAsync()
    {
        if (!string.IsNullOrEmpty(UserInput))
        {
            var response = await _chatService.GetResponseAsync(UserInput);
            ChatHistory.Add(("Utente", UserInput));
            ChatHistory.Add(("Assistente", response));
        }
    }
}

