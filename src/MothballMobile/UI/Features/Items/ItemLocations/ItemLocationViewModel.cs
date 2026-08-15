using CommunityToolkit.Mvvm.Input;
using CoreApp.Contracts;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.Features.Items.ItemLocations;

public partial class ItemLocationViewModel
{
    private readonly INavigationService nav;

    public ItemLocationViewModel(ItemContainerAllocation allocation, INavigationService nav)
    {
        Allocation = allocation ?? throw new ArgumentNullException(nameof(allocation));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
    }

    public ItemContainerAllocation Allocation { get; }

    public string ContainerName => Allocation.ContainerName;

    public string QuantityText => $"Quantity: {Allocation.Quantity}";

    [RelayCommand]
    private Task NavigateAsync()
        => nav.GoToAsync(
            NavigationRoutes.ContainerDetails,
            new Dictionary<string, object>
            {
                [NavigationParams.ContainerId] = Allocation.ContainerId.ToString()
            });
}
