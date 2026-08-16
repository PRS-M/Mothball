using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;

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

    public string ItemsStoredText => $"Items stored: {(ShowQuantityManagement ? Container.ItemCount : Container.Items.Count)}";

    public Task LoadImageAsync()
    {
        return LoadContainerImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateAsync()
    {
        var id = Container.ContainerId.ToString();
        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.ContainerDetails,
            new Dictionary<string, object> { [Infrastructure.NavigationParams.ContainerId] = id }
        );
    }
}
