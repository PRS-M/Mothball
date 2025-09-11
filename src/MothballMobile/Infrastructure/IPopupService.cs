using System.Threading.Tasks;

namespace MothballMobile.Infrastructure;

/// <summary>
/// Abstraction for displaying alerts/prompts to the user.
/// Decouples ViewModels from MAUI Page/Shell APIs.
/// </summary>
public interface IPopupService
{
    /// <summary>
    /// Shows a simple alert with a single cancel/close button.
    /// </summary>
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <summary>
    /// Shows a confirmation dialog. Returns true when the accept button is pressed, false otherwise.
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
