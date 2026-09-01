using System.Globalization;
using MothballMobile.Resources.Localization;

namespace MothballMobile.Infrastructure.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl");
    private CultureInfo culture = ResolveSystemCulture();

    public event EventHandler? LanguageChanged;
    public CultureInfo Culture => culture;

    public string Get(string key)
    {
        var resourceKey = ResourceKeyMap.Get(key);
        return AppResources.ResourceManager.GetString(resourceKey, culture) ?? key;
    }

    public string Format(string key, params object[] args)
        => string.Format(culture, Get(key), args);

    public void SetLanguage(LanguagePreference preference)
    {
        var next = preference switch
        {
            LanguagePreference.Polish => Polish,
            LanguagePreference.English => English,
            _ => ResolveSystemCulture(),
        };
        if (Equals(culture, next)) return;
        culture = next;
        CultureInfo.CurrentUICulture = next;
        CultureInfo.CurrentCulture = next;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static CultureInfo ResolveSystemCulture()
        => CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.Equals("pl", StringComparison.OrdinalIgnoreCase)
            ? Polish
            : English;
}
