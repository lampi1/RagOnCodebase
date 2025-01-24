using System.Globalization;

namespace CodebaseAI.Services
{
    public interface ILocalizationService
    {
        string GetString(string key);
        string GetString(string key, params object[] args);
        CultureInfo CurrentCulture { get; }
        void SetCulture(string cultureName);
    }
}