using System.Globalization;

namespace MothballMobile.Infrastructure.Localization;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;

    CultureInfo Culture { get; }

    string Get(string key);

    string Format(string key, params object[] args);

    void SetLanguage(LanguagePreference preference);
}
