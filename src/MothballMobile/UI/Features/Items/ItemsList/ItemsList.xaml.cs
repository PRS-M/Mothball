using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ItemsList : BasePage
{
    public ItemsList(ItemsListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}