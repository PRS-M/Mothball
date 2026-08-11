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
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeContainers, typeof(UI.Features.Containers.ContainersList.ContainersListPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeItems, typeof(UI.Features.Items.ItemsList.ItemsListPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddContainer, typeof(UI.Features.Containers.AddContainer.AddContainerPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.ContainerDetails, typeof(UI.Features.Containers.ContainerDetails.ContainerDetailsPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.ItemDetails, typeof(UI.Features.Items.ItemDetails.ItemDetailsPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddItem, typeof(UI.Features.Items.AddItem.AddItemPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddExistingItemToContainer, typeof(UI.Features.Containers.AddExistingItemToContainer.AddExistingItemToContainerPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AssociateItemWithContainer, typeof(UI.Features.Containers.AssociateItemWithContainer.AssociateItemWithContainerPage));
	}
}
