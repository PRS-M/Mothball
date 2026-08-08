using MothballMobile.Infrastructure;

namespace MothballMobile;

public partial class App : Application
{
	public App(IAppStartupOrchestrator startupOrchestrator)
	{
		InitializeComponent();

		// Fire-and-forget startup orchestration (init/recovery/error logging).
		_ = startupOrchestrator.StartAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}