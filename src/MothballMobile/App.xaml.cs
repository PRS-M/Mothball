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
		InitializeAppAsync(window);
		return window;
	}

	private async void InitializeAppAsync(Window window)
	{
		try
		{
			await startupOrchestrator.StartAsync();
			window.Page = new AppShell();
		}
		catch (Exception ex)
		{
			Environment.FailFast("Startup initialization failed.", ex);
		}
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