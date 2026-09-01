namespace MothballMobile.Infrastructure.Localization;

public static class Localization
{
    public static ILocalizationService Current { get; private set; } = new LocalizationService();

    public static void Configure(ILocalizationService service)
        => Current = service;
}
