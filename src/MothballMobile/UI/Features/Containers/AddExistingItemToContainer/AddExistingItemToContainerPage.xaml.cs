namespace MothballMobile.UI.Features.Containers.AddExistingItemToContainer;

public partial class AddExistingItemToContainerPage : BasePage
{
    public AddExistingItemToContainerPage(AddExistingItemToContainerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
