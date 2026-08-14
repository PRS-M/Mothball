using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ItemWithPhotosViewModel : ItemWithImagesViewModelBase
{
    private readonly INavigationService navigation;
    private readonly string? sourceContainerId;
    private readonly Guid ownerContainerId;
    private readonly IPopupService popup;
    private readonly Func<Guid, int, Task> saveQuantity;

    public ItemWithPhotosViewModel(
        Item item,
        int quantity,
        Guid ownerContainerId,
        IImagePathResolver paths,
        INavigationService navigation,
        IPopupService popup,
        string? sourceContainerId,
        Func<Guid, int, Task> saveQuantity)
        : base(item, paths)
    {
        this.quantity = quantity;
        this.ownerContainerId = ownerContainerId;
        this.navigation = navigation;
        this.popup = popup;
        this.sourceContainerId = sourceContainerId;
        this.saveQuantity = saveQuantity;
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
            min: 0,
            max: 1000,
            initialValue: Quantity,
            accept: "Set",
            cancel: "Cancel");

        if (selectedQuantity is null || selectedQuantity.Value == Quantity)
        {
            return;
        }

        if (selectedQuantity.Value == 0)
        {
            var confirmed = await popup.ConfirmAsync(
                title: "Remove item",
                message: $"Remove '{Name}' from this container? The item itself will not be deleted.",
                accept: "Remove",
                cancel: "Cancel");

            if (!confirmed)
            {
                return;
            }
        }

        await SaveQuantityAsync(selectedQuantity.Value);
    }

    [RelayCommand]
    private async Task RemoveFromContainerAsync()
    {
        if (ownerContainerId == Guid.Empty)
        {
            return;
        }

        var confirmed = await popup.ConfirmAsync(
            title: "Remove item",
            message: $"Remove '{Name}' from this container? The item itself will not be deleted.",
            accept: "Remove",
            cancel: "Cancel");

        if (!confirmed)
        {
            return;
        }

        await SaveQuantityAsync(0);
    }

    private async Task SaveQuantityAsync(int selectedQuantity)
    {
        await saveQuantity(Item.ItemId, selectedQuantity);
    }
}
