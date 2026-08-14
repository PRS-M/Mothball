using System.Threading.Tasks;
using MothballMobile.Infrastructure.Popups;

namespace MothballMobile.Infrastructure;

/// <summary>
/// Abstraction for displaying alerts/prompts to the user.
/// Decouples ViewModels from MAUI Page/Shell APIs.
/// </summary>
public interface IPopupService
{
    /// <summary>
    /// Shows a simple alert from a reusable popup definition.
    /// </summary>
    Task ShowAlertAsync(AlertPopupDefinition definition);

    /// <summary>
    /// Shows a simple alert with a single cancel/close button.
    /// </summary>
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <summary>
    /// Shows a confirmation dialog from a reusable popup definition.
    /// </summary>
    Task<bool> ConfirmAsync(ConfirmationPopupDefinition definition);

    /// <summary>
    /// Shows a confirmation dialog. Returns true when the accept button is pressed, false otherwise.
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    /// <summary>
    /// Shows a typed list picker from a reusable popup definition.
    /// Returns <c>null</c> when the user cancels.
    /// </summary>
    Task<T?> SelectOptionAsync<T>(OptionPickerPopupDefinition<T> definition);

    /// <summary>
    /// Shows a list picker using an action sheet and returns the selected option.
    /// Returns <c>null</c> when the user cancels.
    /// </summary>
    Task<string?> SelectOptionAsync(string title, string cancel, params string[] options);

    /// <summary>
    /// Shows a modal number picker from a reusable popup definition.
    /// Returns <c>null</c> when the user cancels.
    /// </summary>
    Task<int?> PickNumberAsync(NumberPickerPopupDefinition definition);

    /// <summary>
    /// Shows a modal number picker and returns the selected value.
    /// Returns <c>null</c> when the user cancels.
    /// </summary>
    Task<int?> PickNumberAsync(string title, int min, int max, int initialValue, string accept = "Set", string cancel = "Cancel");
}
