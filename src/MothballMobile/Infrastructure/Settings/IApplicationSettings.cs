namespace MothballMobile.Infrastructure.Settings;

/// <summary>
/// Defines persisted application settings and change notification.
/// </summary>
public interface IApplicationSettings
{
    event EventHandler? AppModeChanged;

    event EventHandler? ThemePaletteChanged;

    AppTheme ThemeOverride { get; set; }

    ThemePalette ThemePalette { get; set; }

    AppMode AppMode { get; set; }

    bool IsAdvancedMode { get; set; }

    bool IsBackupSigningKeyEnabled { get; set; }

    LanguagePreference Language { get; set; }
}
