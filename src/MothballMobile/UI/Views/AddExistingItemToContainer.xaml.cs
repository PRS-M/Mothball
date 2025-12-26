using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class AddExistingItemToContainer : BasePage
{
    public AddExistingItemToContainer(AddExistingItemToContainerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
