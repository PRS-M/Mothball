using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class AssociateItemWithContainer : BasePage
{
    public AssociateItemWithContainer(AssociateItemWithContainerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
