using MothballMobile.UI.Controls;

namespace MothballMobile.Infrastructure;

/// <summary>
/// MAUI-based implementation of popup services using DisplayAlert functionality.
/// </summary>
public sealed class MauiPopupService : IPopupService
{
    /// <inheritdoc />
    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = TryGetCurrentPage();
        if (page is null)
            return Task.CompletedTask;
        return page.DisplayAlertAsync(title, message, cancel);
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = TryGetCurrentPage();
        if (page is null)
            return Task.FromResult(false);
        return page.DisplayAlertAsync(title, message, accept, cancel);
    }

    /// <inheritdoc />
    public async Task<string?> SelectOptionAsync(string title, string cancel, params string[] options)
    {
        var page = TryGetCurrentPage();
        if (page is null)
            return null;

        if (options is null || options.Length == 0)
            return null;

        var selected = await page.DisplayActionSheetAsync(title, cancel, null, options);
        if (string.Equals(selected, cancel, StringComparison.Ordinal))
            return null;

        return selected;
    }

    /// <inheritdoc />
    public async Task<int?> PickNumberAsync(string title, int min, int max, int initialValue, string accept = "Set", string cancel = "Cancel")
    {
        var page = TryGetCurrentPage();
        if (page is null)
            return null;

        if (min > max)
            return null;

        var clampedInitial = Math.Clamp(initialValue, min, max);
        var pickerPage = new NumberPickerModalPage(title, min, max, clampedInitial, accept, cancel);

        await page.Navigation.PushModalAsync(pickerPage, false);
        return await pickerPage.WaitForResultAsync();
    }

    private static Page? TryGetCurrentPage()
    {
        // Prefer Shell.Current for MAUI Shell apps
        if (Shell.Current?.CurrentPage is Page shellPage)
            return shellPage;

        var app = Application.Current;
        if (app?.Windows is { Count: > 0 })
        {
            foreach (var window in app.Windows)
            {
                var page = window?.Page;
                if (page is not null)
                    return page;
            }
        }
        return null;
    }
}
