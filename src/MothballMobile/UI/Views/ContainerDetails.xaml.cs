using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ContainerDetails : ContentPage
{
    public ContainerDetails(ContainerDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
