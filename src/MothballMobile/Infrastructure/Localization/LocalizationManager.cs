namespace MothballMobile.Infrastructure.Localization;

public static class LocalizationManager
{
    public static ILocalizationService Current { get; private set; } = new LocalizationService();

    public static void Configure(ILocalizationService service)
        => Current = service;
}
