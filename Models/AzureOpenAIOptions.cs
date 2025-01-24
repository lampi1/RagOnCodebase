using System.ComponentModel.DataAnnotations;

namespace CodebaseAI.Models;

public class AzureOpenAIOptions
{
    [Required]
    public string ApiKey { get; set; }

    [Required]
    public string EmbeddingEndpoint { get; set; }

    [Required]
    public string CompletionEndpoint { get; set; }
}