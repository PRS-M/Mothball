using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ContainersList : ContentPage
{
    private ContainerListViewModel ViewModel => (ContainerListViewModel)BindingContext;

    public ContainersList(ContainerListViewModel containerListViewModel)
    {
        InitializeComponent();
        BindingContext = containerListViewModel;

        // To automatically fire a command when the page is loaded in MAUI,
        // you can use Behaviors in XAML to invoke a command on appearing.
        // For example, use EventToCommandBehavior from CommunityToolkit.Maui:

        // In your XAML:
        // xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
        // <ContentPage.Behaviors>
        //     <toolkit:EventToCommandBehavior
        //         EventName="Appearing"
        //         Command="{Binding InitializeCommand}" />
        // </ContentPage.Behaviors>

        // This will automatically execute InitializeCommand when the page appears.
        // Remove this.Loaded += ContainersList_Loaded;

        this.Loaded += ContainersList_Loaded;
    }

    private async void ContainersList_Loaded(object? sender, EventArgs e)
    {
        await ViewModel.InitializeAsync();
    }
}