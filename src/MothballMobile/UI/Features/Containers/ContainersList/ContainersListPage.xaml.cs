using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Containers.ContainersList;

public partial class ContainersListPage : BasePage
{
    public ContainersListPage(ContainerListViewModel containerListViewModel)
    {
        InitializeComponent();
        BindingContext = containerListViewModel;
    }
}
