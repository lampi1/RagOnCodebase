using System.ComponentModel.DataAnnotations;

namespace CodebaseAI.Models;

public class AzureOpenAIOptions
{
    [Required(ErrorMessage = "ApiKey is required")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "EmbeddingEndpoint is required")]
    [Url(ErrorMessage = "EmbeddingEndpoint must be a valid URL")]
    public string EmbeddingEndpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "CompletionEndpoint is required")]
    [Url(ErrorMessage = "CompletionEndpoint must be a valid URL")]
    public string CompletionEndpoint { get; set; } = string.Empty;
}