namespace MothballMobile.UI.Controls;

public partial class NumberPickerModalPage : ContentPage
{
    private readonly TaskCompletionSource<int?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int min;
    private readonly int max;

    public NumberPickerModalPage(string title, int min, int max, int initialValue, string accept, string cancel)
    {
        InitializeComponent();

        this.min = min;
        this.max = max;

        TitleLabel.Text = title;
        AcceptButton.Text = accept;
        CancelButton.Text = cancel;

        QuantityEntry.Text = Math.Clamp(initialValue, min, max).ToString();
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
            ShowValidation($"Enter a number between {min} and {max}.");
            return;
        }

        if (parsed < min || parsed > max)
        {
            ShowValidation($"Value must be between {min} and {max}.");
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
        if (!tcs.TrySetResult(value))
        {
            return;
        }

        await Navigation.PopModalAsync(false);
    }

    protected override void OnDisappearing()
    {
        if (!tcs.Task.IsCompleted)
        {
            tcs.TrySetResult(null);
        }

        base.OnDisappearing();
    }
}
