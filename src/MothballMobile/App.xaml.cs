using MothballMobile.Infrastructure;

namespace MothballMobile;

public partial class App : Application
{
	private readonly IAppStartupOrchestrator startupOrchestrator;

	public App(IAppStartupOrchestrator startupOrchestrator)
	{
		InitializeComponent();
		this.startupOrchestrator = startupOrchestrator;
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
			window.Page = new AppShell();
		}
		catch (Exception ex)
		{
			window.Page = CreateStartupErrorPage(window, ex.Message);
		}
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