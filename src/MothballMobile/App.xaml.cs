using CoreApp.Interfaces;

namespace MothballMobile;

public partial class App : Application
{
	public App(IAppStartupInitializer startupInitializer)
	{
		InitializeComponent();

		// Fire-and-forget store init/recovery
		_ = startupInitializer.InitializeAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}