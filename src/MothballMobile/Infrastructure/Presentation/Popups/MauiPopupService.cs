using MothballMobile.UI.Controls;

namespace MothballMobile.Infrastructure.Presentation.Popups;

/// <summary>
/// MAUI-based implementation of popup services using DisplayAlert functionality.
/// </summary>
public sealed class MauiPopupService : IPopupService
{
    /// <inheritdoc />
    public Task ShowAlertAsync(AlertPopupDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return ShowAlertAsync(definition.Title, definition.Message, definition.Cancel);
    }

    /// <inheritdoc />
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = TryGetCurrentPage();
            if (page is not null)
            {
                await page.DisplayAlertAsync(title, message, cancel);
            }
        });
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(ConfirmationPopupDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return ConfirmAsync(definition.Title, definition.Message, definition.Accept, definition.Cancel);
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = TryGetCurrentPage();
            return page is not null
                && await page.DisplayAlertAsync(title, message, accept, cancel);
        });
    }

    /// <inheritdoc />
    public async Task<T?> SelectOptionAsync<T>(OptionPickerPopupDefinition<T> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Options.Count == 0)
        {
            return default;
        }

        var selected = await SelectOptionAsync(
            definition.Title,
            definition.Cancel,
            definition.Options.Select(option => option.Label).ToArray());

        if (string.IsNullOrWhiteSpace(selected))
        {
            return default;
        }

        foreach (var option in definition.Options)
        {
            if (string.Equals(option.Label, selected, StringComparison.Ordinal))
            {
                return option.Value;
            }
        }

        return default;
    }

    /// <inheritdoc />
    public async Task<T?> SelectValueOptionAsync<T>(OptionPickerPopupDefinition<T> definition)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Options.Count == 0)
        {
            return null;
        }

        var selected = await SelectOptionAsync(
            definition.Title,
            definition.Cancel,
            definition.Options.Select(option => option.Label).ToArray());

        if (string.IsNullOrWhiteSpace(selected))
        {
            return null;
        }

        foreach (var option in definition.Options)
        {
            if (string.Equals(option.Label, selected, StringComparison.Ordinal))
            {
                return option.Value;
            }
        }

        return null;
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
    public Task<int?> PickNumberAsync(NumberPickerPopupDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return PickNumberAsync(
            definition.Title,
            definition.Min,
            definition.Max,
            definition.InitialValue,
            definition.Accept,
            definition.Cancel,
            definition.Placeholder,
            definition.Message,
            definition.InvalidNumberMessage,
            definition.OutOfRangeMessage);
    }

    /// <inheritdoc />
    public async Task<int?> PickNumberAsync(string title, int min, int max, int initialValue, string accept = "Set", string cancel = "Cancel")
        => await PickNumberAsync(
            title,
            min,
            max,
            initialValue,
            accept,
            cancel,
            LocalizationManager.Current.Get("Enter quantity"),
            string.Empty,
            LocalizationManager.Current.Format("Enter a number between {0} and {1}.", min, max),
            LocalizationManager.Current.Format("Value must be between {0} and {1}.", min, max));

    private async Task<int?> PickNumberAsync(
        string title,
        int min,
        int max,
        int initialValue,
        string accept,
        string cancel,
        string placeholder,
        string message,
        string invalidNumberMessage,
        string outOfRangeMessage)
    {
        var page = TryGetCurrentPage();
        if (page is null)
            return null;

        if (min > max)
            return null;

        var clampedInitial = Math.Clamp(initialValue, min, max);
        var pickerPage = new NumberPickerModalPage(
            title,
            min,
            max,
            clampedInitial,
            accept,
            cancel,
            placeholder,
            message,
            invalidNumberMessage,
            outOfRangeMessage);

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
