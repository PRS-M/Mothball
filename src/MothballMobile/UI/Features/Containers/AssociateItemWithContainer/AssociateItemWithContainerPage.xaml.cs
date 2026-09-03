namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public partial class AssociateItemWithContainerPage : BasePage
{
    public AssociateItemWithContainerPage(AssociateItemWithContainerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
