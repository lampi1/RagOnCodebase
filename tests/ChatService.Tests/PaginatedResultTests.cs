using Xunit;
using CodebaseAI.Models;

namespace CodebaseAI.Tests
{
    public class PaginatedResultTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var items = new[] { "item1", "item2", "item3" };
            var pageNumber = 2;
            var pageSize = 3;
            var totalCount = 10;

            // Act
            var result = new PaginatedResult<string>(items, pageNumber, pageSize, totalCount);

            // Assert
            Assert.Equal(items, result.Items);
            Assert.Equal(pageNumber, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(totalCount, result.TotalCount);
            Assert.Equal(4, result.TotalPages); // 10 items / 3 per page = 4 pages (ceiling)
        }

        [Fact]
        public void HasPreviousPage_ReturnsCorrectValue()
        {
            // Arrange & Act
            var resultPage1 = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 20);
            var resultPage2 = new PaginatedResult<string>(Array.Empty<string>(), 2, 10, 20);

            // Assert
            Assert.False(resultPage1.HasPreviousPage);
            Assert.True(resultPage2.HasPreviousPage);
        }

        [Fact]
        public void HasNextPage_ReturnsCorrectValue()
        {
            // Arrange & Act
            var resultLastPage = new PaginatedResult<string>(Array.Empty<string>(), 2, 10, 20);
            var resultNotLastPage = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 20);

            // Assert
            Assert.False(resultLastPage.HasNextPage);
            Assert.True(resultNotLastPage.HasNextPage);
        }

        [Theory]
        [InlineData(0, 1)] // Invalid page number
        [InlineData(1, 0)] // Invalid page size
        public void Constructor_WithInvalidParameters_ThrowsArgumentException(int pageNumber, int pageSize)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new PaginatedResult<string>(Array.Empty<string>(), pageNumber, pageSize, 0));
        }

        [Fact]
        public void TotalPages_WithZeroTotalCount_ReturnsOne()
        {
            // Arrange & Act
            var result = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 0);

            // Assert
            Assert.Equal(1, result.TotalPages);
        }

        [Fact]
        public void TotalPages_CalculatesCorrectly()
        {
            // Arrange & Act
            var result1 = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 95); // 10 pages
            var result2 = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 100); // 10 pages
            var result3 = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 101); // 11 pages

            // Assert
            Assert.Equal(10, result1.TotalPages);
            Assert.Equal(10, result2.TotalPages);
            Assert.Equal(11, result3.TotalPages);
        }
    }
}