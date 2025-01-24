using System;
using Xunit;
using CodebaseAI.Models;

namespace CodebaseAI.Tests
{
    public class SearchOptionsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var options = new SearchOptions();
            
            Assert.Equal(10, options.PageSize);
            Assert.Equal(1, options.PageNumber);
            Assert.Equal(SearchSortOption.Relevance, options.SortBy);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PageSize_WhenSetToInvalidValue_UsesDefaultValue(int invalidSize)
        {
            var options = new SearchOptions { PageSize = invalidSize };
            Assert.Equal(10, options.PageSize);
        }

        [Fact]
        public void PageSize_WhenSetAboveMaximum_LimitsToMaxValue()
        {
            var options = new SearchOptions { PageSize = 200 };
            Assert.Equal(100, options.PageSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PageNumber_WhenSetToInvalidValue_UsesMinimumValue(int invalidPage)
        {
            var options = new SearchOptions { PageNumber = invalidPage };
            Assert.Equal(1, options.PageNumber);
        }

        [Fact]
        public void Validate_WithValidOptions_DoesNotThrow()
        {
            var options = new SearchOptions
            {
                PageSize = 50,
                PageNumber = 2,
                SortBy = SearchSortOption.DateDescending
            };

            var exception = Record.Exception(() => options.Validate());
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_WithInvalidPageNumber_ThrowsArgumentException()
        {
            var options = new SearchOptions { PageNumber = -1 };
            Assert.Throws<ArgumentException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithTooLargePageSize_ThrowsArgumentException()
        {
            var options = new SearchOptions { PageSize = 101 };
            Assert.Throws<ArgumentException>(() => options.Validate());
        }
    }
}