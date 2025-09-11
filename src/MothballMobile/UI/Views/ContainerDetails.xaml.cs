using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ContainerDetails : BasePage
{
    public ContainerDetails(ContainerDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
