using Microsoft.Maui.Storage;

namespace MothballMobile.Infrastructure;

public sealed class ApplicationSettings(IPreferences preferences) : IApplicationSettings
{
    private const string AppModeKey = "AppMode";

    public event EventHandler? AppModeChanged;

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
