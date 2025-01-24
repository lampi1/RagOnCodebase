using System.Globalization;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;
using CodebaseAI.Services;
using CodebaseAI.Resources;

namespace CodebaseAI.Tests
{
    public class LocalizationServiceTests
    {
        private readonly Mock<IStringLocalizer<ErrorMessages>> _localizerMock;
        private readonly LocalizationService _localizationService;

        public LocalizationServiceTests()
        {
            _localizerMock = new Mock<IStringLocalizer<ErrorMessages>>();
            _localizationService = new LocalizationService(_localizerMock.Object);
        }

        [Fact]
        public void GetString_WithoutParams_ReturnsLocalizedString()
        {
            // Arrange
            var key = "TestKey";
            var expectedValue = "Test Value";
            _localizerMock.Setup(x => x[key]).Returns(new LocalizedString(key, expectedValue));

            // Act
            var result = _localizationService.GetString(key);

            // Assert
            Assert.Equal(expectedValue, result);
            _localizerMock.Verify(x => x[key], Times.Once);
        }

        [Fact]
        public void GetString_WithParams_ReturnsFormattedLocalizedString()
        {
            // Arrange
            var key = "TestKey";
            var param = "Test Param";
            var expectedValue = "Test Value: Test Param";
            _localizerMock.Setup(x => x[key, It.IsAny<object[]>()]).Returns(new LocalizedString(key, expectedValue));

            // Act
            var result = _localizationService.GetString(key, param);

            // Assert
            Assert.Equal(expectedValue, result);
            _localizerMock.Verify(x => x[key, It.IsAny<object[]>()], Times.Once);
        }

        [Fact]
        public void SetCulture_UpdatesCurrentCulture()
        {
            // Arrange
            var cultureName = "it";
            var expectedCulture = new CultureInfo(cultureName);

            // Act
            _localizationService.SetCulture(cultureName);

            // Assert
            Assert.Equal(expectedCulture, _localizationService.CurrentCulture);
            Assert.Equal(expectedCulture, CultureInfo.CurrentCulture);
            Assert.Equal(expectedCulture, CultureInfo.CurrentUICulture);
        }

        [Fact]
        public void SetCulture_InvalidCulture_ThrowsCultureNotFoundException()
        {
            // Arrange
            var invalidCultureName = "invalid-culture";

            // Act & Assert
            Assert.Throws<CultureNotFoundException>(() => _localizationService.SetCulture(invalidCultureName));
        }
    }
}
