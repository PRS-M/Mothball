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
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeContainers, typeof(UI.Views.ContainersList));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeItems, typeof(UI.Views.ItemsList));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddContainer, typeof(UI.Views.AddContainer));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.ContainerDetails, typeof(UI.Views.ContainerDetails));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.ItemDetails, typeof(UI.Views.ItemDetails));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddItem, typeof(UI.Views.AddItem));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddExistingItemToContainer, typeof(UI.Views.AddExistingItemToContainer));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AssociateItemWithContainer, typeof(UI.Views.AssociateItemWithContainer));
	}
}
