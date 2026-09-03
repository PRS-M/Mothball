using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure.Scanning;
#if IOS || ANDROID
using Plugin.AdMob.Services;
#endif

namespace MothballMobile.Infrastructure.Startup;

/// <summary>
/// Coordinates the window-level startup lifecycle after the application host is created.
/// </summary>
public sealed class AppStartupCoordinator
{
    private readonly IAppStartupOrchestrator startupOrchestrator;
    private readonly IBackupSignatureSecretProvider backupSignatureSecretProvider;
    private readonly IPopupService popup;
    private readonly AdMobSettings adMobSettings;
    private readonly ILogger<AppStartupCoordinator> logger;
    private readonly ILogger<AppShell> appShellLogger;
    private readonly BarcodeLookupCoordinator barcodeLookupCoordinator;

    public AppStartupCoordinator(
        IAppStartupOrchestrator startupOrchestrator,
        IBackupSignatureSecretProvider backupSignatureSecretProvider,
        IPopupService popup,
        AdMobSettings adMobSettings,
        ILogger<AppStartupCoordinator> logger,
        ILogger<AppShell> appShellLogger,
        BarcodeLookupCoordinator barcodeLookupCoordinator)
    {
        this.startupOrchestrator = startupOrchestrator ?? throw new ArgumentNullException(nameof(startupOrchestrator));
        this.backupSignatureSecretProvider = backupSignatureSecretProvider ?? throw new ArgumentNullException(nameof(backupSignatureSecretProvider));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.adMobSettings = adMobSettings ?? throw new ArgumentNullException(nameof(adMobSettings));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.appShellLogger = appShellLogger ?? throw new ArgumentNullException(nameof(appShellLogger));
        this.barcodeLookupCoordinator = barcodeLookupCoordinator ?? throw new ArgumentNullException(nameof(barcodeLookupCoordinator));
    }

    /// <summary>
    /// Creates the temporary page shown while startup is in progress.
    /// </summary>
    public Page CreateStartupPage()
        => new ContentPage
        {
            BackgroundColor = GetActiveColor("Background", "#FAF8FF"),
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 12,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new ActivityIndicator { IsRunning = true, WidthRequest = 44, HeightRequest = 44 },
                    new Label
                    {
                        Text = LocalizationManager.Current.Get("Starting Mothball..."),
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };

    /// <summary>
    /// Runs startup and replaces the window page with the application shell or a retry page.
    /// </summary>
    /// <param name="window">The application window whose page is being initialized.</param>
    public async Task InitializeAsync(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        try
        {
            await backupSignatureSecretProvider.GetOrCreateAsync();
            await startupOrchestrator.StartAsync();
            var shell = new AppShell(popup, appShellLogger, barcodeLookupCoordinator);
            var shellLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            shell.Loaded += OnShellLoaded;
            window.Page = shell;
            await shellLoaded.Task;
            await ShowStartupAdAsync();

            void OnShellLoaded(object? sender, EventArgs args)
            {
                shell.Loaded -= OnShellLoaded;
                shellLoaded.TrySetResult();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Application startup failed.");
            window.Page = CreateStartupErrorPage(window, ex.Message);
        }
    }

    private async Task ShowStartupAdAsync()
    {
#if IOS || ANDROID
        var appOpenAdService = IPlatformApplication.Current?.Services.GetService<IAppOpenAdService>();
        if (appOpenAdService is null)
        {
            return;
        }

        if (appOpenAdService.IsAdLoaded)
        {
            appOpenAdService.ShowAd();
            return;
        }

        var adLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnAdLoaded(object? sender, EventArgs args) => adLoaded.TrySetResult(true);

        appOpenAdService.OnAdLoaded += OnAdLoaded;
        try
        {
            appOpenAdService.PrepareAd(adMobSettings.AppOpenAdUnitId);
            await Task.WhenAny(adLoaded.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            if (appOpenAdService.IsAdLoaded)
            {
                appOpenAdService.ShowAd();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Startup app-open ad failed.");
        }
        finally
        {
            appOpenAdService.OnAdLoaded -= OnAdLoaded;
        }
#else
        await Task.CompletedTask;
#endif
    }

    private Page CreateStartupErrorPage(Window window, string message)
    {
        var retryButton = new Button
        {
            Text = LocalizationManager.Current.Get("Retry startup")
        };

        retryButton.Clicked += async (_, _) =>
        {
            retryButton.IsEnabled = false;
            window.Page = CreateStartupPage();
            await InitializeAsync(window);
        };

        return new ContentPage
        {
            BackgroundColor = GetActiveColor("Background", "#FAF8FF"),
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 12,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = LocalizationManager.Current.Get("Startup failed"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = message,
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    retryButton
                }
            }
        };
    }

    private static Color GetActiveColor(string resourceKey, string fallback)
        => Application.Current?.Resources[resourceKey] as Color ?? Color.FromArgb(fallback);
}
