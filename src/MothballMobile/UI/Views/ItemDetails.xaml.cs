using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ItemDetails : ContentPage
{
    public ItemDetails(ItemDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
