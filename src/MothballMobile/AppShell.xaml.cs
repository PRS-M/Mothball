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
		Routing.RegisterRoute("AddItem", typeof(UI.Views.AddItem));
		Routing.RegisterRoute("AddExistingItemToContainer", typeof(UI.Views.AddExistingItemToContainer));
		Routing.RegisterRoute("AssociateItemWithContainer", typeof(UI.Views.AssociateItemWithContainer));
	}
}
