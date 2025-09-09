using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ContainersList : ContentPage
{
    private ContainerListViewModel ViewModel => (ContainerListViewModel)BindingContext;

    public ContainersList(ContainerListViewModel containerListViewModel)
    {
        InitializeComponent();
        BindingContext = containerListViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeAsync();
    }
}