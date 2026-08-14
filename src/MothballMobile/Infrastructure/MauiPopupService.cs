using MothballMobile.UI.Controls;
using MothballMobile.Infrastructure.Popups;

namespace MothballMobile.Infrastructure;

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
    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = TryGetCurrentPage();
        if (page is null)
            return Task.CompletedTask;
        return page.DisplayAlertAsync(title, message, cancel);
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(ConfirmationPopupDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return ConfirmAsync(definition.Title, definition.Message, definition.Accept, definition.Cancel);
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
            "Enter quantity",
            $"Enter a number between {min} and {max}.",
            $"Value must be between {min} and {max}.");

    private async Task<int?> PickNumberAsync(
        string title,
        int min,
        int max,
        int initialValue,
        string accept,
        string cancel,
        string placeholder,
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
