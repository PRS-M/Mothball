using System.Diagnostics;

namespace MothballMobile;

public partial class AppShell : Shell
{
	public AppShell(Infrastructure.IPhotoBackgroundOperationTracker photoBackgroundOperationTracker)
	{
		InitializeComponent();
		BindingContext = photoBackgroundOperationTracker;
		RegisterRoutes();
	}

	private async void OnBackgroundOperationsBannerTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			await GoToAsync(Infrastructure.NavigationRoutes.BackgroundOperations);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Background operations navigation failed: {ex}");
			// Best-effort UX: ignore navigation failures from banner tap.
		}
	}

	private static void RegisterRoutes()
	{
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeContainers, typeof(UI.Features.Containers.ContainersList.ContainersListPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeItems, typeof(UI.Features.Items.ItemsList.ItemsListPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.Settings, typeof(UI.Features.Settings.SettingsPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddContainer, typeof(UI.Features.Containers.AddContainer.AddContainerPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.ContainerDetails, typeof(UI.Features.Containers.ContainerDetails.ContainerDetailsPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.ItemDetails, typeof(UI.Features.Items.ItemDetails.ItemDetailsPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddItem, typeof(UI.Features.Items.AddItem.AddItemPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddExistingItemToContainer, typeof(UI.Features.Containers.AddExistingItemToContainer.AddExistingItemToContainerPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.AssociateItemWithContainer, typeof(UI.Features.Containers.AssociateItemWithContainer.AssociateItemWithContainerPage));
		Routing.RegisterRoute(Infrastructure.NavigationRoutes.BackgroundOperations, typeof(UI.Features.BackgroundOperations.BackgroundOperationsPage));
	}
}
