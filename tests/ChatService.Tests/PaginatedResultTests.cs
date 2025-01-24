using Xunit;
using CodebaseAI.Models;

namespace CodebaseAI.Tests
{
    public class PaginatedResultTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var items = new[] { "item1", "item2" };
            var result = new PaginatedResult<string>(items, 2, 10, 25);

            Assert.Equal(items, result.Items);
            Assert.Equal(2, result.PageNumber);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(3, result.TotalPages); // 25 items with 10 per page = 3 pages
        }

        [Fact]
        public void TotalPages_WithZeroTotalCount_ReturnsOne()
        {
            var result = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 0);
            Assert.Equal(1, result.TotalPages);
        }

        [Fact]
        public void TotalPages_WithPartialPage_RoundsUp()
        {
            var result = new PaginatedResult<string>(Array.Empty<string>(), 1, 10, 21);
            Assert.Equal(3, result.TotalPages); // 21 items with 10 per page = 3 pages (last page partial)
        }

        [Fact]
        public void Constructor_WithNullItems_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PaginatedResult<string>(null, 1, 10, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidPageNumber_ThrowsArgumentException(int invalidPageNumber)
        {
            Assert.Throws<ArgumentException>(() => 
                new PaginatedResult<string>(Array.Empty<string>(), invalidPageNumber, 10, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidPageSize_ThrowsArgumentException(int invalidPageSize)
        {
            Assert.Throws<ArgumentException>(() => 
                new PaginatedResult<string>(Array.Empty<string>(), 1, invalidPageSize, 0));
        }

        [Theory]
        [InlineData(-1)]
        public void Constructor_WithInvalidTotalCount_ThrowsArgumentException(int invalidTotalCount)
        {
            Assert.Throws<ArgumentException>(() => 
                new PaginatedResult<string>(Array.Empty<string>(), 1, 10, invalidTotalCount));
        }
    }
}