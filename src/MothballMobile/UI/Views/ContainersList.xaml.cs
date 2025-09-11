using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ContainersList : BasePage
{
    public ContainersList(ContainerListViewModel containerListViewModel)
    {
        InitializeComponent();
        BindingContext = containerListViewModel;
    }
}