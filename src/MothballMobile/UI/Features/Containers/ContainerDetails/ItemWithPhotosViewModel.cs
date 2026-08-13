using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;
using System.Threading.Tasks;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ItemWithPhotosViewModel : ItemWithImagesViewModelBase
{
    private readonly INavigationService navigation;
    private readonly string? sourceContainerId;
    private readonly Guid ownerContainerId;
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IPopupService popup;
    private readonly Func<Guid, int, Task>? onQuantitySaved;

    public ItemWithPhotosViewModel(
        Item item,
        int quantity,
        Guid ownerContainerId,
        IImagePathResolver paths,
        INavigationService navigation,
        IInventoryCommandRepository inventoryCommands,
        IPopupService popup,
        string? sourceContainerId,
        Func<Guid, int, Task>? onQuantitySaved = null)
        : base(item, paths)
    {
        this.quantity = quantity;
        this.ownerContainerId = ownerContainerId;
        this.navigation = navigation;
        this.inventoryCommands = inventoryCommands;
        this.popup = popup;
        this.sourceContainerId = sourceContainerId;
        this.onQuantitySaved = onQuantitySaved;
    }

    [ObservableProperty]
    private int quantity;

    public Task LoadImagesAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = Item.ItemId.ToString()
        };

        if (!string.IsNullOrWhiteSpace(sourceContainerId))
        {
            parameters[NavigationParams.ContainerId] = sourceContainerId;
        }

        return navigation.GoToAsync(
            NavigationRoutes.ItemDetails,
            parameters);
    }

    [RelayCommand]
    private async Task EditQuantityAsync()
    {
        if (ownerContainerId == Guid.Empty)
        {
            return;
        }

        var selectedQuantity = await popup.PickNumberAsync(
            title: "Set quantity",
            min: 1,
            max: 1000,
            initialValue: Quantity,
            accept: "Set",
            cancel: "Cancel");

        if (selectedQuantity is null || selectedQuantity.Value == Quantity)
        {
            return;
        }

        await inventoryCommands.ReplaceItemContainerRelationQuantity(Item.ItemId, ownerContainerId, selectedQuantity.Value);
        Quantity = selectedQuantity.Value;

        if (onQuantitySaved is not null)
        {
            await onQuantitySaved(Item.ItemId, selectedQuantity.Value);
        }
    }
}
