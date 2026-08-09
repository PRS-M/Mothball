using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ContainerDetailsPage : BasePage
{
    public ContainerDetailsPage(ContainerDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
