using Microsoft.Extensions.Logging;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Base class for a settings page section, providing shared busy-state and failure-alert handling.
/// </summary>
public abstract class SettingsSectionViewModelBase : BaseViewModel
{
    private readonly ILogger logger;

    protected SettingsSectionViewModelBase(IPopupService popup, IPopupDefinitionService popupDefinitions, ILogger logger)
    {
        Popup = popup;
        PopupDefinitions = popupDefinitions;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected IPopupService Popup { get; }

    protected IPopupDefinitionService PopupDefinitions { get; }

    /// <summary>Runs an operation, logging and alerting the given failure popup on exception instead of propagating it.</summary>
    protected async Task TryWithAlertAsync(
        Func<Task> action,
        string logMessage,
        Func<string, AlertPopupDefinition> onFailure,
        params object?[] logArgs)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, logMessage, logArgs);
            await Popup.ShowAlertAsync(onFailure(ex.Message));
        }
    }
}
