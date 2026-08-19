using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if IOS || ANDROID
using Plugin.AdMob.Services;
#endif

namespace MothballMobile;

public partial class App : Application
{
	private readonly IAppStartupOrchestrator startupOrchestrator;
	private readonly IPhotoBackgroundOperationTracker photoBackgroundOperationTracker;
	private readonly IApplicationSettings applicationSettings;
	private readonly IBackupSignatureSecretProvider backupSignatureSecretProvider;
	private readonly AdMobSettings adMobSettings;
	private readonly ILogger<App> logger;
	private readonly ILogger<AppShell> appShellLogger;

	public App(
		IAppStartupOrchestrator startupOrchestrator,
		IPhotoBackgroundOperationTracker photoBackgroundOperationTracker,
		IApplicationSettings applicationSettings,
		IBackupSignatureSecretProvider backupSignatureSecretProvider,
		AdMobSettings adMobSettings,
		ILogger<App> logger,
		ILogger<AppShell> appShellLogger)
	{
		InitializeComponent();
		this.startupOrchestrator = startupOrchestrator;
		this.photoBackgroundOperationTracker = photoBackgroundOperationTracker;
		this.applicationSettings = applicationSettings;
		this.backupSignatureSecretProvider = backupSignatureSecretProvider;
		this.adMobSettings = adMobSettings;
		UserAppTheme = applicationSettings.ThemeOverride;
		this.logger = logger;
		this.appShellLogger = appShellLogger;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(CreateStartupPage());
		_ = InitializeAppAsync(window);
		return window;
	}

	private async Task InitializeAppAsync(Window window)
	{
		try
		{
			await backupSignatureSecretProvider.GetOrCreateAsync();
			await startupOrchestrator.StartAsync();
			var shell = new AppShell(photoBackgroundOperationTracker, appShellLogger);
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

	private static Page CreateStartupErrorPage(Window window, string message)
	{
		var retryButton = new Button
		{
			Text = "Retry startup"
		};

		retryButton.Clicked += async (_, _) =>
		{
			retryButton.IsEnabled = false;
			window.Page = CreateStartupPage();
			if (window.Page is not null)
			{
				var app = Application.Current as App;
				if (app is not null)
				{
					await app.InitializeAppAsync(window);
				}
			}
		};

		return new ContentPage
		{
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
						Text = "Startup failed",
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

	private static Page CreateStartupPage()
	{
		return new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 12,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center,
				Children =
				{
					new ActivityIndicator
					{
						IsRunning = true,
						WidthRequest = 44,
						HeightRequest = 44
					},
					new Label
					{
						Text = "Starting Mothball...",
						HorizontalTextAlignment = TextAlignment.Center
					}
				}
			}
		};
	}
}
