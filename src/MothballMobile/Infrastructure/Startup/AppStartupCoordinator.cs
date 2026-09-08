using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
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
    private static readonly TimeSpan ShellLoadedTimeout = TimeSpan.FromSeconds(5);

    private readonly IAppStartupOrchestrator startupOrchestrator;
    private readonly IBackupSignatureSecretProvider backupSignatureSecretProvider;
    private readonly IPopupService popup;
    private readonly AdMobSettings adMobSettings;
    private readonly ILogger<AppStartupCoordinator> logger;
    private readonly ILogger<AppShell> appShellLogger;
    private readonly BarcodeLookupCoordinator barcodeLookupCoordinator;
    private ProgressBar? overallProgressBar;
    private ProgressBar? stepProgressBar;
    private Label? startupStatusLabel;

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
    {
        overallProgressBar = new ProgressBar
        {
            Progress = 0,
            WidthRequest = 280,
            HeightRequest = 8,
            ProgressColor = GetActiveColor("Primary", "#496B9F"),
            BackgroundColor = GetActiveColor("OutlineVariant", "#D0CCD8"),
        };
        stepProgressBar = new ProgressBar
        {
            Progress = 0,
            WidthRequest = 280,
            HeightRequest = 8,
            ProgressColor = GetActiveColor("Primary", "#496B9F"),
            BackgroundColor = GetActiveColor("OutlineVariant", "#D0CCD8"),
        };
        startupStatusLabel = new Label
        {
            Text = LocalizationManager.Current.Get("Preparing startup"),
            HorizontalTextAlignment = TextAlignment.Center,
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
                    new ActivityIndicator { IsRunning = true, WidthRequest = 44, HeightRequest = 44 },
                    overallProgressBar,
                    stepProgressBar,
                    startupStatusLabel,
                }
            }
        };
    }

    /// <summary>
    /// Runs startup and replaces the window page with the application shell or a retry page.
    /// </summary>
    /// <param name="window">The application window whose page is being initialized.</param>
    public async Task InitializeAsync(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        try
        {
            var startupStarted = Stopwatch.GetTimestamp();
            logger.LogInformation("Application startup started.");
            IProgress<StartupProgress> progress = new Progress<StartupProgress>(ReportStartupProgress);
            progress.Report(new StartupProgress(0.02, 0, "Preparing startup"));

            var secretStarted = Stopwatch.GetTimestamp();
            progress.Report(new StartupProgress(0.05, 0, "Preparing secure storage"));
            await backupSignatureSecretProvider.GetOrCreateAsync();
            progress.Report(new StartupProgress(0.18, 1, "Preparing secure storage"));
            logger.LogInformation(
                "Application startup signing key completed in {ElapsedMilliseconds:F0} ms.",
                Stopwatch.GetElapsedTime(secretStarted).TotalMilliseconds);

            var persistenceStarted = Stopwatch.GetTimestamp();
#if DEBUG
            const bool automaticDemoSeeding = true;
#else
            const bool automaticDemoSeeding = false;
#endif
            await startupOrchestrator.StartAsync(progress, automaticDemoSeeding);
            logger.LogInformation(
                "Application startup persistence completed in {ElapsedMilliseconds:F0} ms.",
                Stopwatch.GetElapsedTime(persistenceStarted).TotalMilliseconds);

            var shell = new AppShell(popup, appShellLogger, barcodeLookupCoordinator);
            progress.Report(new StartupProgress(0.9, 0, "Loading application interface"));
            var shellLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            shell.Loaded += OnShellLoaded;
            try
            {
                window.Page = shell;
                await shellLoaded.Task.WaitAsync(ShellLoadedTimeout);
            }
            catch (TimeoutException)
            {
                // A missed Loaded event must not leave the user on the splash page forever.
                logger.LogWarning(
                    "AppShell did not raise Loaded within {TimeoutSeconds} seconds; continuing startup.",
                    ShellLoadedTimeout.TotalSeconds);
            }
            finally
            {
                shell.Loaded -= OnShellLoaded;
            }

            progress.Report(new StartupProgress(0.95, 1, "Loading application interface"));

            progress.Report(new StartupProgress(0.96, 0, "Preparing advertising"));
            await ShowStartupAdAsync();
            progress.Report(new StartupProgress(0.99, 1, "Preparing advertising"));
            progress.Report(new StartupProgress(1, 1, "Startup complete"));
            logger.LogInformation(
                "Application startup completed in {ElapsedMilliseconds:F0} ms.",
                Stopwatch.GetElapsedTime(startupStarted).TotalMilliseconds);

            void OnShellLoaded(object? sender, EventArgs args)
            {
                shellLoaded.TrySetResult();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Application startup failed.");
            window.Page = CreateStartupErrorPage(window, ex.Message);
        }
    }

    private void ReportStartupProgress(StartupProgress progress)
    {
        var overallFraction = Math.Clamp(progress.OverallFraction, 0, 1);
        var stepFraction = Math.Clamp(progress.StepFraction, 0, 1);
        var status = LocalizationManager.Current.Get(progress.Status);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (overallProgressBar is not null)
            {
                overallProgressBar.Progress = overallFraction;
            }

            if (stepProgressBar is not null)
            {
                stepProgressBar.Progress = stepFraction;
            }

            if (startupStatusLabel is not null)
            {
                startupStatusLabel.Text = status;
            }
        });
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
        => Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
}
