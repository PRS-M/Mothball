using Microsoft.Maui.Controls;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.Views;

public class BasePage : ContentPage
{
    private IDisposable? previousDisposable;

    protected override void OnBindingContextChanged()
    {
        var old = previousDisposable;
        base.OnBindingContextChanged();
        previousDisposable = BindingContext as IDisposable;
        if (old is not null && !ReferenceEquals(old, previousDisposable))
        {
            old.Dispose();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IInitializable init)
        {
            await init.InitializeAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is IDisposable d)
        {
            d.Dispose();
        }
    }
}
