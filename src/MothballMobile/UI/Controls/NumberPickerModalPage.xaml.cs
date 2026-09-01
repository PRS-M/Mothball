namespace MothballMobile.UI.Controls;

#if IOS || MACCATALYST
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
#endif

public partial class NumberPickerModalPage : ContentPage
{
    private readonly TaskCompletionSource<int?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int min;
    private readonly int max;
    private readonly string invalidNumberMessage;
    private readonly string outOfRangeMessage;
    private bool isClosing;

    public NumberPickerModalPage(
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
        InitializeComponent();

#if IOS || MACCATALYST
        // Present over the existing page so the background stays visible under the scrim.
    this.On<iOS>().SetModalPresentationStyle(Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.UIModalPresentationStyle.OverFullScreen);
#endif

        this.min = min;
        this.max = max;
        this.invalidNumberMessage = invalidNumberMessage;
        this.outOfRangeMessage = outOfRangeMessage;

        TitleLabel.Text = title;
        AcceptButton.Text = accept;
        CancelButton.Text = cancel;
        QuantityEntry.Placeholder = placeholder;
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrWhiteSpace(message);

        QuantityEntry.Text = Math.Clamp(initialValue, min, max).ToString(Localization.Current.Culture);
        QuantityEntry.CursorPosition = QuantityEntry.Text.Length;
        QuantityEntry.SelectionLength = 0;
    }

    public Task<int?> WaitForResultAsync() => tcs.Task;

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(null);
    }

    private async void OnBackdropTapped(object? sender, TappedEventArgs e)
    {
        await CloseAsync(null);
    }

    private async void OnEntryCompleted(object? sender, EventArgs e)
    {
        await TryAcceptAsync();
    }

    private async void OnAcceptClicked(object? sender, EventArgs e)
    {
        await TryAcceptAsync();
    }

    private async Task TryAcceptAsync()
    {
        var raw = QuantityEntry.Text?.Trim();
        if (!int.TryParse(raw, out var parsed))
        {
            ShowValidation(invalidNumberMessage);
            return;
        }

        if (parsed < min || parsed > max)
        {
            ShowValidation(outOfRangeMessage);
            return;
        }

        HideValidation();
        await CloseAsync(parsed);
    }

    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.IsVisible = true;
    }

    private void HideValidation()
    {
        ValidationLabel.Text = string.Empty;
        ValidationLabel.IsVisible = false;
    }

    private async Task CloseAsync(int? value)
    {
        if (isClosing || tcs.Task.IsCompleted)
        {
            return;
        }

        isClosing = true;
        try
        {
            await Navigation.PopModalAsync(false);
        }
        finally
        {
            tcs.TrySetResult(value);
        }
    }

    protected override void OnDisappearing()
    {
        if (!isClosing && !tcs.Task.IsCompleted)
        {
            tcs.TrySetResult(null);
        }

        base.OnDisappearing();
    }
}
