using System.ComponentModel.DataAnnotations;

namespace CodebaseAI.Models
{
    public class AzureOpenAIOptions
    {
        [Required(ErrorMessage = "Azure OpenAI API Key is required")]
        public string ApiKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Azure OpenAI Embedding Endpoint is required")]
        [RegularExpression(@"^https?://.*", ErrorMessage = "Embedding Endpoint must be a valid URL")]
        public string EmbeddingEndpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Azure OpenAI Completion Endpoint is required")]
        [RegularExpression(@"^https?://.*", ErrorMessage = "Completion Endpoint must be a valid URL")]
        public string CompletionEndpoint { get; set; } = string.Empty;
    }
}