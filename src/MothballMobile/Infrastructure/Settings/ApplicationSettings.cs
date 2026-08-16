using Microsoft.Maui.Storage;

namespace MothballMobile.Infrastructure.Settings;

public sealed class ApplicationSettings(IPreferences preferences) : IApplicationSettings
{
    private const string AppModeKey = "AppMode";
    private const string ThemeOverrideKey = "ThemeOverride";

    public event EventHandler? AppModeChanged;

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
}
