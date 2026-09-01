using Microsoft.Maui.Storage;
using MothballMobile.Infrastructure.Localization;

namespace MothballMobile.Infrastructure.Settings;

public sealed class ApplicationSettings(IPreferences preferences) : IApplicationSettings
{
    private const string AppModeKey = "AppMode";
    private const string ThemeOverrideKey = "ThemeOverride";
    private const string ThemePaletteKey = "ThemePalette";
    private const string ThemePaletteConfiguredKey = "ThemePaletteConfigured";
    private const string BackupSigningKeyEnabledKey = "BackupSigningKeyEnabled";
    private const string LanguageKey = "Language";

    public event EventHandler? AppModeChanged;
    public event EventHandler? ThemePaletteChanged;

    public LanguagePreference Language
    {
        get
        {
            var raw = preferences.Get(LanguageKey, nameof(LanguagePreference.System));
            return Enum.TryParse<LanguagePreference>(raw, out var language)
                ? language
                : LanguagePreference.System;
        }
        set
        {
            if (Language == value)
            {
                return;
            }

            preferences.Set(LanguageKey, value.ToString());
        }
    }

    public AppTheme ThemeOverride
    {
        get
        {
            var raw = preferences.Get(ThemeOverrideKey, nameof(AppTheme.Unspecified));
            return Enum.TryParse<AppTheme>(raw, out var theme)
                ? theme
                : AppTheme.Unspecified;
        }
        set
        {
            if (ThemeOverride == value)
            {
                return;
            }

            preferences.Set(ThemeOverrideKey, value.ToString());
        }
    }

    public ThemePalette ThemePalette
    {
        get
        {
            if (!preferences.Get(ThemePaletteConfiguredKey, defaultValue: false))
            {
                preferences.Set(ThemePaletteKey, nameof(ThemePalette.BlueprintLedger));
                preferences.Set(ThemePaletteConfiguredKey, true);
                return ThemePalette.BlueprintLedger;
            }

            var raw = preferences.Get(ThemePaletteKey, nameof(ThemePalette.BlueprintLedger));
            return Enum.TryParse<ThemePalette>(raw, out var palette)
                ? palette
                : ThemePalette.BlueprintLedger;
        }
        set
        {
            if (ThemePalette == value)
            {
                return;
            }

            preferences.Set(ThemePaletteKey, value.ToString());
            preferences.Set(ThemePaletteConfiguredKey, true);
            ThemePaletteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public AppMode AppMode
    {
        get
        {
            var raw = preferences.Get(AppModeKey, nameof(AppMode.Advanced));
            return Enum.TryParse<AppMode>(raw, out var mode)
                ? mode
                : AppMode.Advanced;
        }
        set
        {
            if (AppMode == value)
            {
                return;
            }

            preferences.Set(AppModeKey, value.ToString());
            AppModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsAdvancedMode
    {
        get => AppMode == AppMode.Advanced;
        set => AppMode = value ? AppMode.Advanced : AppMode.Simple;
    }

    public bool IsBackupSigningKeyEnabled
    {
        get => preferences.Get(BackupSigningKeyEnabledKey, defaultValue: true);
        set => preferences.Set(BackupSigningKeyEnabledKey, value);
    }
}
