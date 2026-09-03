namespace MothballMobile.Infrastructure.Settings;

public enum AppMode
{
    /// <summary>Personal Storage with the simple presentation.</summary>
    PersonalStorageSimple,
    /// <summary>Personal Storage with quantity-management workflows.</summary>
    PersonalStorageAdvanced,
    /// <summary>Opt-in experimental warehouse-management presentation.</summary>
    WmsExperimental,

    // Legacy aliases keep source compatibility for existing callers.
    Simple = PersonalStorageSimple,
    Advanced = PersonalStorageAdvanced,
}

/// <summary>Provides mode-specific capability decisions without exposing settings storage to features.</summary>
public static class AppModeCapabilities
{
    /// <summary>Gets whether Personal Storage quantity workflows are available.</summary>
    public static bool SupportsAdvancedPersonalStorage(AppMode mode)
        => mode == AppMode.PersonalStorageAdvanced;

    /// <summary>Gets whether the experimental WMS experience is available.</summary>
    public static bool SupportsExperimentalWms(AppMode mode)
        => mode == AppMode.WmsExperimental;
}
