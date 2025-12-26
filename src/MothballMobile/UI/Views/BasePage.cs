using Microsoft.Maui.Controls;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.Views;

public class BasePage : ContentPage
{
    private IDisposable? previousDisposable;

    /// <summary>
    /// Handles changes to the <see cref="P:Microsoft.Maui.Controls.BindableObject.BindingContext"/>.
    /// Disposes the previous binding context if it implements <see cref="IDisposable"/> and
    /// caches the current one for later cleanup.
    /// </summary>
    /// <remarks>
    /// Always calls the base implementation before updating the cached reference.
    /// </remarks>
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

    /// <summary>
    /// Invoked when the page becomes visible.
    /// If the binding context implements <see cref="MothballMobile.Infrastructure.IInitializable"/>,
    /// runs its asynchronous initialization via <see cref="MothballMobile.Infrastructure.IInitializable.InitializeAsync"/>.
    /// </summary>
    /// <remarks>
    /// This method awaits initialization but returns <c>void</c> because it overrides a framework hook.
    /// </remarks>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IInitializable init)
        {
            await init.InitializeAsync();
        }
    }

    /// <summary>
    /// Invoked when the page is no longer visible.
    /// Disposes the current binding context if it implements <see cref="IDisposable"/> to release resources.
    /// </summary>
    /// <remarks>
    /// Always calls the base implementation first.
    /// </remarks>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is IDisposable d)
        {
            d.Dispose();
        }
    }
}
