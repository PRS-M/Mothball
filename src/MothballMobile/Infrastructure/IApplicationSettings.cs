namespace MothballMobile.Infrastructure;

public interface IApplicationSettings
{
    event EventHandler? AppModeChanged;

    AppMode AppMode { get; set; }

    bool IsAdvancedMode { get; set; }
}
