using System.Collections.Generic;

namespace CodebaseAI.Models
{
    /// <summary>
    /// Represents a paginated result set containing items and pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the result set.</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Gets the collection of items for the current page.
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets the current page number (1-based).
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        public int TotalItems { get; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Initializes a new instance of the PaginatedResult class.
        /// </summary>
        /// <param name="items">The items for the current page.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalItems">The total number of items across all pages.</param>
        public PaginatedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalItems)
        {
            Items = new List<T>(items);
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalItems = totalItems;
        }
    }
}