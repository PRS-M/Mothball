namespace MothballMobile.UI.Features.Items.ItemsList;

public partial class ItemsListPage : BasePage
{
    public ItemsListPage(ItemsListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
