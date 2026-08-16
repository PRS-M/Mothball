using MothballMobile.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if IOS || ANDROID
using Plugin.AdMob.Services;
#endif

namespace MothballMobile;

public partial class App : Application
{
#if IOS
	private const string AppOpenTestAdUnitId = "ca-app-pub-3940256099942544/5575463023";
#elif ANDROID
	private const string AppOpenTestAdUnitId = "ca-app-pub-3940256099942544/9257395921";
#endif
	private readonly IAppStartupOrchestrator startupOrchestrator;
	private readonly IPhotoBackgroundOperationTracker photoBackgroundOperationTracker;
	private readonly IApplicationSettings applicationSettings;
	private readonly ILogger<App> logger;
	private readonly ILogger<AppShell> appShellLogger;

	public App(
		IAppStartupOrchestrator startupOrchestrator,
		IPhotoBackgroundOperationTracker photoBackgroundOperationTracker,
		IApplicationSettings applicationSettings,
		ILogger<App> logger,
		ILogger<AppShell> appShellLogger)
	{
		InitializeComponent();
		this.startupOrchestrator = startupOrchestrator;
		this.photoBackgroundOperationTracker = photoBackgroundOperationTracker;
		this.applicationSettings = applicationSettings;
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
			await startupOrchestrator.StartAsync();
			window.Page = new AppShell(photoBackgroundOperationTracker, appShellLogger);

			await Task.Yield();
			await ShowStartupAdAsync();
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
			appOpenAdService.PrepareAd(AppOpenTestAdUnitId);
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
