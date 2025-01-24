using System.ComponentModel.DataAnnotations;

namespace CodebaseAI.Models
{
    public class ElasticsearchOptions
    {
        [Required(ErrorMessage = "Elasticsearch API Key is required")]
        public string ApiKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Elasticsearch Cloud ID is required")]
        public string CloudId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Elasticsearch Cloud Endpoint is required")]
        [RegularExpression(@"^https?://.*", ErrorMessage = "Cloud Endpoint must be a valid URL")]
        public string CloudEndpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Default Index name is required")]
        public string DefaultIndex { get; set; } = "codebase_index_v2";
    }
}