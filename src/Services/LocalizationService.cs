using System.Globalization;
using Microsoft.Extensions.Localization;
using CodebaseAI.Resources;

namespace CodebaseAI.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly IStringLocalizer<ErrorMessages> _localizer;
        private CultureInfo _currentCulture;

        public LocalizationService(IStringLocalizer<ErrorMessages> localizer)
        {
            _localizer = localizer;
            _currentCulture = CultureInfo.CurrentCulture;
        }

        public CultureInfo CurrentCulture => _currentCulture;

        public string GetString(string key)
        {
            return _localizer[key];
        }

        public string GetString(string key, params object[] args)
        {
            return _localizer[key, args];
        }

        public void SetCulture(string cultureName)
        {
            var culture = new CultureInfo(cultureName);
            _currentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }
}