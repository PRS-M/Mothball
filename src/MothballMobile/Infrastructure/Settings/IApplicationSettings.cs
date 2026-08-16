namespace MothballMobile.Infrastructure.Settings;

/// <summary>
/// Defines persisted application settings and change notification.
/// </summary>
public interface IApplicationSettings
{
    event EventHandler? AppModeChanged;

    AppTheme ThemeOverride { get; set; }

    AppMode AppMode { get; set; }

    bool IsAdvancedMode { get; set; }
}
