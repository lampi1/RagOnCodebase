using System.ComponentModel.DataAnnotations;

namespace CodebaseAI.Models;

/// <summary>
/// Represents the sorting options available for search results.
/// </summary>
public enum SearchSortOption
{
    /// <summary>
    /// Sort by relevance score (default)
    /// </summary>
    Relevance,

    /// <summary>
    /// Sort by date in ascending order
    /// </summary>
    DateAscending,

    /// <summary>
    /// Sort by date in descending order
    /// </summary>
    DateDescending,

    /// <summary>
    /// Sort by file name
    /// </summary>
    FileName
}

/// <summary>
/// Represents the options for searching documents.
/// </summary>
public class SearchOptions
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    private int _pageSize = DefaultPageSize;
    private int _pageNumber = 1;

    /// <summary>
    /// Gets or sets the page size for pagination. Maximum value is 100.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value <= 0 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    /// <summary>
    /// Gets or sets the page number for pagination. Minimum value is 1.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value <= 0 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the sort option for the search results.
    /// </summary>
    public SearchSortOption SortBy { get; set; } = SearchSortOption.Relevance;

    /// <summary>
    /// Validates the search options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(this);

        if (!Validator.TryValidateObject(this, context, validationResults, true))
        {
            throw new ArgumentException(
                string.Join(Environment.NewLine, validationResults.Select(r => r.ErrorMessage)));
        }

        if (PageSize > MaxPageSize)
        {
            throw new ArgumentException($"Page size cannot be greater than {MaxPageSize}.");
        }

        if (PageNumber < 1)
        {
            throw new ArgumentException("Page number must be greater than 0.");
        }
    }
}