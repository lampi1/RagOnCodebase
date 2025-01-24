using System.ComponentModel.DataAnnotations;

namespace CodebaseAI.Models;

public class ElasticsearchOptions
{
    [Required(ErrorMessage = "ApiKey is required")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "CloudId is required")]
    public string CloudId { get; set; } = string.Empty;

    [Required(ErrorMessage = "CloudEndpoint is required")]
    [Url(ErrorMessage = "CloudEndpoint must be a valid URL")]
    public string CloudEndpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "DefaultIndex is required")]
    public string DefaultIndex { get; set; } = "codebase_index_v2";
}