namespace MothballMobile.Infrastructure.Presentation.Popups;

public static class PopupServiceExtensions
{
    /// <summary>Shows a confirmation popup and runs the action only when the user confirms.</summary>
    /// <returns><see langword="true"/> when the user confirmed and the action ran; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> ConfirmAndRunAsync(
        this IPopupService popup,
        ConfirmationPopupDefinition definition,
        Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(popup);
        ArgumentNullException.ThrowIfNull(action);

        if (!await popup.ConfirmAsync(definition))
        {
            return false;
        }

        await action();
        return true;
    }
}
