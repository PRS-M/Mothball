using CommunityToolkit.Mvvm.ComponentModel;
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
        LoadContainerImages();
    }

    public bool ShowQuantityManagement { get; }

    [ObservableProperty]
    private bool isSelected;

    public string ItemTypesStoredText => LocalizationManager.Current.Format("Item types stored: {0}", Container.ItemTypeCount);

    public string ItemsStoredText => LocalizationManager.Current.Format("Items stored (Total): {0}", ShowQuantityManagement ? Container.TotalItemQuantity : Container.ItemTypeCount);

    [RelayCommand]
    private Task NavigateAsync()
    {
        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.ContainerDetails,
            new Infrastructure.Navigation.ContainerDetailsNavigationRequest(Container.ContainerId)
        );
    }
}
