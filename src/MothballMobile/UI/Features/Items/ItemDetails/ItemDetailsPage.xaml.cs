using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Items.ItemDetails;

public partial class ItemDetailsPage : BasePage
{
    public ItemDetailsPage(ItemDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
