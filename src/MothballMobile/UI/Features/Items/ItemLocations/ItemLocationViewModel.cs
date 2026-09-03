using CoreApp.Domain.Entities.InventoryAggregate;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;

namespace MothballMobile.UI.Features.Items.ItemLocations;


public partial class ItemLocationViewModel : ContainerWithImagesViewModelBase
{
    private readonly INavigationService nav;

    public ItemLocationViewModel(
        Container container,
        ItemContainerAllocation allocation,
        IImagePathResolver paths,
        INavigationService nav,
        bool showQuantityManagement)
        : base(container, paths)
    {
        Allocation = allocation ?? throw new ArgumentNullException(nameof(allocation));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
        ShowQuantityManagement = showQuantityManagement;
    }

    public ItemContainerAllocation Allocation { get; }
    public bool ShowQuantityManagement { get; }

    public new string ItemCount => LocalizationManager.Current.Format("Quantity here: {0}", Allocation.Quantity);

    public Task LoadImagesAsync()
        => LoadContainerImagesAsync(clearFirst: true);

    [RelayCommand]
    private Task NavigateAsync()
        => nav.GoToAsync(
            NavigationRoutes.ContainerDetails,
            new Infrastructure.Navigation.ContainerDetailsNavigationRequest(Allocation.ContainerId));
}
