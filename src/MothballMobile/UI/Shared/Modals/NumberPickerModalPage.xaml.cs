namespace MothballMobile.UI.Shared.Modals;

public partial class NumberPickerModalPage : ContentPage
{
    private readonly TaskCompletionSource<int?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int min;

    public NumberPickerModalPage(string title, int min, int max, int initialValue, string accept, string cancel)
    {
        InitializeComponent();

        this.min = min;

        TitleLabel.Text = title;
        AcceptButton.Text = accept;
        CancelButton.Text = cancel;

        for (var value = min; value <= max; value++)
        {
            QuantityPicker.Items.Add(value.ToString());
        }

        QuantityPicker.SelectedIndex = Math.Clamp(initialValue - min, 0, QuantityPicker.Items.Count - 1);
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

    private async void OnAcceptClicked(object? sender, EventArgs e)
    {
        if (QuantityPicker.SelectedIndex < 0)
        {
            await CloseAsync(null);
            return;
        }

        await CloseAsync(min + QuantityPicker.SelectedIndex);
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
