using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
#if IOS || ANDROID
using Plugin.AdMob;
#endif

namespace MothballMobile.UI.Shared;

public class BasePage : ContentPage
{
#if IOS
    private const string BannerTestAdUnitId = "ca-app-pub-3940256099942544/2934735716";
#elif ANDROID
    private const string BannerTestAdUnitId = "ca-app-pub-3940256099942544/6300978111";
#endif
    private IDisposable? previousDisposable;
    private readonly SemaphoreSlim initializationGate = new(1, 1);
    private bool contentWrappedWithAdBanner;

    public BasePage()
    {
        Loaded += (_, _) => WrapContentWithAdBanner();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        WrapContentWithAdBanner();
    }

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
        WrapContentWithAdBanner();

        if (BindingContext is IInitializable init)
        {
            await initializationGate.WaitAsync();
            try
            {
                await init.InitializeAsync();
            }
            catch (Exception ex)
            {
                MauiLogger.For(GetType(), this)
                    ?.LogError(ex, "Page initialization failed for {PageType}.", GetType().Name);
            }
            finally
            {
                initializationGate.Release();
            }
        }
    }

    /// <summary>
    /// Invoked when the page is no longer visible.
    /// Reserved for page-level lifecycle hooks.
    /// </summary>
    /// <remarks>
    /// Always calls the base implementation first.
    /// </remarks>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private void WrapContentWithAdBanner()
    {
#if IOS || ANDROID
        if (contentWrappedWithAdBanner || Content is null)
        {
            return;
        }

        var pageContent = Content;
        var layout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        Content = null;
        Grid.SetRow(pageContent, 0);
        layout.Add(pageContent);

        var banner = CreateAdBannerContent();

        Grid.SetRow(banner, 1);
        layout.Add(banner);

        contentWrappedWithAdBanner = true;
        Content = layout;
#endif
    }

#if IOS || ANDROID
    private static View CreateAdBannerContent()
    {
        var bannerHost = new Grid
        {
            HeightRequest = 60,
            MinimumHeightRequest = 50,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            IsClippedToBounds = true,
            BackgroundColor = Colors.Transparent
        };

        var banner = new BannerAd
        {
#if IOS || ANDROID
            AdUnitId = BannerTestAdUnitId,
#endif
            HeightRequest = 60,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        var placeholder = CreateDevelopmentAdPlaceholder();

        banner.OnAdLoaded += (_, _) => placeholder.IsVisible = false;
        banner.OnAdFailedToLoad += (_, _) => placeholder.IsVisible = true;

        bannerHost.Add(banner);
        bannerHost.Add(placeholder);

        return bannerHost;
    }

    private static View CreateDevelopmentAdPlaceholder()
    {
        return new Border
        {
            HeightRequest = 60,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Colors.LightGray),
            BackgroundColor = Color.FromArgb("#F2F2F2"),
            Padding = new Thickness(12, 0),
            Content = new Label
            {
                Text = "Test Ad",
                FontSize = 12,
                TextColor = Colors.DimGray,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
    }
#endif
}
