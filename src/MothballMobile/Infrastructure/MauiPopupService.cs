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
        return page.DisplayAlert(title, message, cancel);
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = TryGetCurrentPage();
        if (page is null)
            return Task.FromResult(false);
        return page.DisplayAlert(title, message, accept, cancel);
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
