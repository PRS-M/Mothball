namespace MothballMobile.UI.Features.Items.ItemLocations;

public partial class ItemLocationsPage : BasePage
{
    public ItemLocationsPage(ItemLocationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
