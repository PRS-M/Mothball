using CoreApp.Domain.Entities.InventoryAggregate;
﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Contracts;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ItemWithPhotosViewModel : ItemWithImagesViewModelBase
{
    private readonly INavigationService navigation;
    private readonly string? sourceContainerId;
    private readonly Guid ownerContainerId;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly Func<Guid, int, Task> saveQuantity;
    private readonly Func<Guid, Guid, Task> consume;
    private readonly Action skipNextInitialization;

    public ItemWithPhotosViewModel(
        ContainerItemInventoryEntry entry,
        Guid ownerContainerId,
        IImagePathResolver paths,
        INavigationService navigation,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        string? sourceContainerId,
        bool showQuantityManagement,
        Func<Guid, int, Task> saveQuantity,
        Func<Guid, Guid, Task> consume,
        Action skipNextInitialization)
        : base(entry.Inventory, paths)
    {
        quantity = entry.ContainerQuantity;
        this.ownerContainerId = ownerContainerId;
        this.navigation = navigation;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.sourceContainerId = sourceContainerId;
        this.saveQuantity = saveQuantity;
        this.consume = consume;
        this.skipNextInitialization = skipNextInitialization;
        ShowQuantityManagement = showQuantityManagement;
    }

    [ObservableProperty]
    private int quantity;

    public bool ShowQuantityManagement { get; }

    public Task LoadImagesAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        Guid? parsedSourceContainerId = Guid.TryParse(this.sourceContainerId, out var parsedContainerId)
            ? parsedContainerId
            : null;

        return navigation.GoToAsync(
            NavigationRoutes.ItemDetails,
            new Infrastructure.Navigation.ItemDetailsNavigationRequest(Item.ItemId, parsedSourceContainerId));
    }

    [RelayCommand]
    private async Task EditQuantityAsync()
    {
        if (!ShowQuantityManagement)
        {
            return;
        }

        if (ownerContainerId == Guid.Empty)
        {
            return;
        }

        skipNextInitialization();
        var selectedQuantity = await popup.PickNumberAsync(popupDefinitions.SetQuantity(Quantity));

        if (selectedQuantity is null || selectedQuantity.Value == Quantity)
        {
            return;
        }

        if (selectedQuantity.Value == 0)
        {
            skipNextInitialization();
            var confirmed = await popup.ConfirmAsync(popupDefinitions.RemoveItemFromContainer(Name));

            if (!confirmed)
            {
                return;
            }
        }

        await SaveQuantityAsync(selectedQuantity.Value);
    }

    [RelayCommand]
    private Task RemoveFromContainerAsync()
    {
        if (ownerContainerId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        skipNextInitialization();
        return popup.ConfirmAndRunAsync(popupDefinitions.RemoveItemFromContainer(Name), () => SaveQuantityAsync(0));
    }

    [RelayCommand]
    private async Task UseAsync()
    {
        if (!ShowQuantityManagement || ownerContainerId == Guid.Empty)
        {
            return;
        }

        skipNextInitialization();
        try
        {
            await consume(Item.ItemId, ownerContainerId);
        }
        catch (Exception ex)
        {
            await popup.ShowAlertAsync(popupDefinitions.InventoryQuantityUpdateFailed(ex.Message));
        }
    }

    private async Task SaveQuantityAsync(int selectedQuantity)
    {
        await saveQuantity(Item.ItemId, selectedQuantity);
    }
}
