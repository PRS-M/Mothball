namespace MothballMobile.Infrastructure;

public interface IApplicationSettings
{
    event EventHandler? AppModeChanged;

    AppTheme ThemeOverride { get; set; }

    AppMode AppMode { get; set; }

    bool IsAdvancedMode { get; set; }
}
