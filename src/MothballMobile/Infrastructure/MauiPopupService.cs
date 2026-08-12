using Microsoft.Maui.Controls.Shapes;

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

    private sealed class NumberPickerModalPage : ContentPage
    {
        private readonly TaskCompletionSource<int?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Picker picker;

        public NumberPickerModalPage(string title, int min, int max, int initialValue, string accept, string cancel)
        {
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.35f);

            picker = new Picker
            {
                Title = "Select quantity",
                HorizontalOptions = LayoutOptions.Fill,
            };

            for (var value = min; value <= max; value++)
            {
                picker.Items.Add(value.ToString());
            }

            picker.SelectedIndex = Math.Clamp(initialValue - min, 0, picker.Items.Count - 1);

            var cancelButton = new Button
            {
                Text = cancel,
                HorizontalOptions = LayoutOptions.Fill,
            };
            cancelButton.Clicked += async (_, _) => await CloseAsync(null);

            var acceptButton = new Button
            {
                Text = accept,
                HorizontalOptions = LayoutOptions.Fill,
            };
            acceptButton.Clicked += async (_, _) =>
            {
                if (picker.SelectedIndex < 0)
                {
                    await CloseAsync(null);
                    return;
                }

                if (int.TryParse(picker.Items[picker.SelectedIndex], out var number))
                {
                    await CloseAsync(number);
                    return;
                }

                await CloseAsync(null);
            };

            Content = new Grid
            {
                Padding = 20,
                Children =
                {
                    new Border
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Padding = 16,
                        StrokeThickness = 1,
                        StrokeShape = new RoundRectangle { CornerRadius = 12 },
                        BackgroundColor = Colors.White,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 12,
                            WidthRequest = 280,
                            Children =
                            {
                                new Label
                                {
                                    Text = title,
                                    FontAttributes = FontAttributes.Bold,
                                    HorizontalTextAlignment = TextAlignment.Center,
                                },
                                picker,
                                BuildButtonsGrid(cancelButton, acceptButton)
                            }
                        }
                    }
                }
            };
        }

        public Task<int?> WaitForResultAsync() => tcs.Task;

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

        private static Grid BuildButtonsGrid(Button cancelButton, Button acceptButton)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 10,
            };

            grid.Children.Add(cancelButton);
            Grid.SetColumn(cancelButton, 0);

            grid.Children.Add(acceptButton);
            Grid.SetColumn(acceptButton, 1);

            return grid;
        }
    }
}
