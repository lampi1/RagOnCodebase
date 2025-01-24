using System.ComponentModel.DataAnnotations;

namespace CodebaseAI.Models;

public class ElasticsearchOptions
{
    [Required]
    public string ApiKey { get; set; }

    [Required]
    public string CloudId { get; set; }

    [Required]
    public string CloudEndpoint { get; set; }

    public string DefaultIndex { get; set; } = "codebase_index_v2";
}
