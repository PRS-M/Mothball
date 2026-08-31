using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;

namespace MothballMobile.UI.Features.Containers.ContainersList;

public partial class ContainerViewModel : ContainerWithImagesViewModelBase
{
    private readonly INavigationService nav;

    public ContainerViewModel(
        Container container,
        IImagePathResolver paths,
        INavigationService nav,
        bool showQuantityManagement)
        : base(container, paths)
    {
        this.nav = nav;
        ShowQuantityManagement = showQuantityManagement;
    }

    public bool ShowQuantityManagement { get; }

    public string ItemTypesStoredText => $"Item types stored: {Container.ItemTypeCount}";

    public string ItemsStoredText => $"Items stored (Total): {(ShowQuantityManagement ? Container.TotalItemQuantity : Container.ItemTypeCount)}";

    public Task LoadImageAsync()
    {
        return LoadContainerImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateAsync()
    {
        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.ContainerDetails,
            new Infrastructure.Navigation.ContainerDetailsNavigationRequest(Container.ContainerId)
        );
    }
}
