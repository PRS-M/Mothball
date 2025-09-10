namespace MothballMobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		RegisterRoutes();
	}

	private static void RegisterRoutes()
	{
		Routing.RegisterRoute("AddContainer", typeof(UI.Views.AddContainer));
		Routing.RegisterRoute("ContainerDetails", typeof(UI.Views.ContainerDetails));
		Routing.RegisterRoute("ItemDetails", typeof(UI.Views.ItemDetails));
	}
}
