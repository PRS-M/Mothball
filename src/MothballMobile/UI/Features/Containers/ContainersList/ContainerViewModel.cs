using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;

namespace MothballMobile.UI.ViewModels;

public partial class ContainerViewModel : ContainerWithImagesViewModelBase
{
    private readonly Infrastructure.INavigationService nav;

    public ContainerViewModel(Container container, IImagePathResolver paths, Infrastructure.INavigationService nav)
        : base(container, paths)
    {
        this.nav = nav;
    }

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
