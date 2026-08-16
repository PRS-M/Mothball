using CoreApp.Entities.Inventory;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;

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

    public new string ItemCount => $"Quantity here: {Allocation.Quantity}";

    public Task LoadImagesAsync()
        => LoadContainerImagesAsync(clearFirst: true);

    [RelayCommand]
    private Task NavigateAsync()
        => nav.GoToAsync(
            NavigationRoutes.ContainerDetails,
            new Dictionary<string, object>
            {
                [NavigationParams.ContainerId] = Allocation.ContainerId.ToString()
            });
}
