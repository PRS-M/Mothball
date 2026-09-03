using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure.BackgroundOperations.Photos;
using Microsoft.Maui.Controls.Shapes;

#if IOS || ANDROID
using Plugin.AdMob;
#endif

namespace MothballMobile.UI.Shared;


public class BasePage : ContentPage
{
    private IDisposable? previousDisposable;
    private BaseViewModel? errorSource;
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
        if (errorSource is not null)
        {
            errorSource.ErrorOccurred -= OnViewModelErrorOccurred;
        }

        base.OnBindingContextChanged();
        previousDisposable = BindingContext as IDisposable;
        errorSource = BindingContext as BaseViewModel;
        if (errorSource is not null)
        {
            errorSource.ErrorOccurred += OnViewModelErrorOccurred;
        }

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
                await ShowGenericErrorAsync();
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

    private static async void OnViewModelErrorOccurred(string message)
    {
        var popup = IPlatformApplication.Current?.Services.GetService<IPopupService>();
        if (popup is not null)
        {
            await popup.ShowAlertAsync(LocalizationManager.Current.Get("Error"), message);
        }
    }

    private static async Task ShowGenericErrorAsync()
    {
        var popup = IPlatformApplication.Current?.Services.GetService<IPopupService>();
        if (popup is not null)
        {
            await popup.ShowAlertAsync(
                LocalizationManager.Current.Get("Error"),
                LocalizationManager.Current.Get("Something went wrong. Please try again."));
        }
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
		var adMobSettings = IPlatformApplication.Current?.Services.GetRequiredService<AdMobSettings>()
			?? throw new InvalidOperationException("AdMob settings are not available.");
		var photoTracker = IPlatformApplication.Current.Services.GetRequiredService<IPhotoBackgroundOperationTracker>();
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
    			AdUnitId = adMobSettings.BannerAdUnitId,
            HeightRequest = 60,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        var placeholder = CreateDevelopmentAdPlaceholder();

        banner.OnAdLoaded += (_, _) => placeholder.IsVisible = false;
        banner.OnAdFailedToLoad += (_, _) => placeholder.IsVisible = true;

        var adContent = new Grid();
        adContent.Add(banner);
        adContent.Add(placeholder);

        var progressBar = new ProgressBar
        {
            HorizontalOptions = LayoutOptions.Fill
        };
        progressBar.SetBinding(ProgressBar.ProgressProperty, nameof(IPhotoBackgroundOperationTracker.OverallProgress));

        var progressContent = new Border
        {
            Padding = new Thickness(12, 6),
            Background = new SolidColorBrush(Color.FromArgb("#F2F2F2")),
            Stroke = new SolidColorBrush(Colors.LightGray),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = LocalizationManager.Current.Get("Image processing"),
                        FontSize = 12,
                        TextColor = Colors.DimGray,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    progressBar
                }
            }
        };

        bannerHost.BindingContext = photoTracker;
        adContent.SetBinding(IsVisibleProperty, new Binding(
            nameof(IPhotoBackgroundOperationTracker.IsProcessing),
            converter: new InverseBooleanConverter()));
        progressContent.SetBinding(IsVisibleProperty, nameof(IPhotoBackgroundOperationTracker.IsProcessing));

        bannerHost.Add(adContent);
        bannerHost.Add(progressContent);

        return bannerHost;
    }

    private sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is bool isProcessing && !isProcessing;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
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
                Text = LocalizationManager.Current.Get("Test Ad"),
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
