using System.Globalization;
using MothballMobile.UI.Features.Settings;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Localization;

[TestFixture]
[NonParallelizable]
public sealed class LocalizationServiceTests
{
    private CultureInfo? defaultCulture;
    private CultureInfo? defaultUiCulture;
    private CultureInfo? currentCulture;
    private CultureInfo? currentUiCulture;

    [SetUp]
    public void SetUp()
    {
        defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        currentCulture = CultureInfo.CurrentCulture;
        currentUiCulture = CultureInfo.CurrentUICulture;
    }

    [TearDown]
    public void TearDown()
    {
        CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = defaultUiCulture;
        CultureInfo.CurrentCulture = currentCulture!;
        CultureInfo.CurrentUICulture = currentUiCulture!;
    }

    [TestCase(LanguagePreference.English, "en")]
    [TestCase(LanguagePreference.Polish, "pl")]
    [TestCase(LanguagePreference.German, "de")]
    [TestCase(LanguagePreference.Spanish, "es")]
    public void SetLanguage_AppliesRequestedCultureToCurrentAndDefaultThreads(LanguagePreference preference, string expectedLanguage)
    {
        var service = new LocalizationService();

        service.SetLanguage(preference);

        Assert.Multiple(() =>
        {
            Assert.That(service.Culture.TwoLetterISOLanguageName, Is.EqualTo(expectedLanguage));
            Assert.That(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Is.EqualTo(expectedLanguage));
            Assert.That(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Is.EqualTo(expectedLanguage));
            Assert.That(CultureInfo.DefaultThreadCurrentCulture!.TwoLetterISOLanguageName, Is.EqualTo(expectedLanguage));
            Assert.That(CultureInfo.DefaultThreadCurrentUICulture!.TwoLetterISOLanguageName, Is.EqualTo(expectedLanguage));
        });
    }

    [Test]
    public void LanguagePreference_PersistsAndIsReadByTheLanguageSelector()
    {
        var preferences = new MemoryPreferences();
        var settings = new ApplicationSettings(preferences)
        {
            Language = LanguagePreference.German,
        };
        LocalizationManager.Configure(new LocalizationService());

        var viewModel = new AppearanceSettingsViewModel(settings);

        Assert.Multiple(() =>
        {
            Assert.That(preferences.Get("Language", string.Empty), Is.EqualTo("German"));
            Assert.That(viewModel.SelectedLanguageOption, Is.EqualTo("German (AI-Translated)"));
        });

        viewModel.SelectedLanguageOption = "Spanish (AI-Translated)";

        Assert.That(settings.Language, Is.EqualTo(LanguagePreference.Spanish));
    }

    [Test]
    public void BarcodeExtendedMode_IsDisabledByDefaultAndPersistsChanges()
    {
        var preferences = new MemoryPreferences();
        var settings = new ApplicationSettings(preferences);

        Assert.That(settings.IsBarcodeExtendedMode, Is.False);

        settings.IsBarcodeExtendedMode = true;

        Assert.Multiple(() =>
        {
            Assert.That(preferences.Get("BarcodeExtendedMode", false), Is.True);
            Assert.That(new ApplicationSettings(preferences).IsBarcodeExtendedMode, Is.True);
        });
    }

    private sealed class MemoryPreferences : IPreferences
    {
        private readonly Dictionary<string, string> values = [];

        public string Get(string key, string defaultValue)
            => values.TryGetValue(key, out var value) ? value : defaultValue;

        public void Set(string key, string value)
            => values[key] = value;

        public bool Get(string key, bool defaultValue)
            => values.TryGetValue(key, out var value) && bool.TryParse(value, out var result)
                ? result
                : defaultValue;

        public void Set(string key, bool value)
            => values[key] = value.ToString();
    }
}
