using MothballMobile.Infrastructure;

namespace MothballMobile;

public partial class App : Application
{
	public App(MothballDatabase database)
	{
		InitializeComponent();

		// Fire-and-forget DB init
		_ = database.InitializeAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}