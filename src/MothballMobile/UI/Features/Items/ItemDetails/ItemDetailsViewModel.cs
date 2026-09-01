using CoreApp.Domain.Entities.InventoryAggregate;
﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.ItemAggregate;
using Microsoft.Extensions.Logging;

namespace MothballMobile.UI.Features.Items.ItemDetails;

public partial class ItemDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable
{
    private readonly ItemDetailsCoordinator itemDetailsCoordinator;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private Item? currentItem;
    private IReadOnlyList<ItemContainerAllocation> currentAllocations = [];
    private string? sourceContainerId;

    [ObservableProperty]
    private string itemId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string descriptionDraft = string.Empty;

    [ObservableProperty]
    private bool isEditingDescription;

    [ObservableProperty]
    private int totalQuantity;

    [ObservableProperty]
    private int assignedQuantity;

    [ObservableProperty]
    private int unassignedQuantity;

    [ObservableProperty]
    private string? containerId;

    public bool HasNoContainerRelation => string.IsNullOrWhiteSpace(this.ContainerId);
    public bool HasContainerRelation => !HasNoContainerRelation;
    public bool HasUnassignedQuantity => UnassignedQuantity > 0;
    public bool ShowQuantityManagement => applicationSettings.IsAdvancedMode;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public string DisplayDescription => HasDescription ? Description : "No description.";
    public bool IsViewingDescription => !IsEditingDescription;
    public bool ShowGoToContainerButton => HasContainerRelation
        && (string.IsNullOrWhiteSpace(sourceContainerId)
            || currentAllocations.Count > 1
            || !string.Equals(ContainerId, sourceContainerId, StringComparison.OrdinalIgnoreCase));

    public ObservableCollection<string> ImagePaths { get; } = new();

    public ItemDetailsViewModel(
        ItemDetailsCoordinator itemDetailsCoordinator,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IImagePathResolver paths,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ImageService imageService,
        IPhotoBackgroundOperationTracker photoBackgroundOperationTracker,
        IBackgroundTaskObserver backgroundTasks)
        : base(paths, imageService, popup, popupDefinitions, photoBackgroundOperationTracker)
    {
        this.itemDetailsCoordinator = itemDetailsCoordinator;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.backgroundTasks = backgroundTasks;
    }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        sourceContainerId = null;

        if (query.TryGetValue(nameof(ItemId), out var val) && val is string id && !string.IsNullOrWhiteSpace(id))
        {
            ItemId = id;
        }

        if (query.TryGetValue(NavigationParams.ContainerId, out var sourceValue)
            && sourceValue is string sourceId
            && !string.IsNullOrWhiteSpace(sourceId))
        {
            sourceContainerId = sourceId;
        }

        NotifyContainerRelationStateChanged();
    }

    partial void OnUnassignedQuantityChanged(int value)
        => OnPropertyChanged(nameof(HasUnassignedQuantity));

    partial void OnDescriptionChanged(string value)
    {
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(DisplayDescription));
    }

    partial void OnIsEditingDescriptionChanged(bool value)
        => OnPropertyChanged(nameof(IsViewingDescription));

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            return Task.CompletedTask;
        }

        return InitializeAsync(ItemId);
    }

    /// <summary>
    /// Loads item details, photos, inventory, and allocation state for the specified item.
    /// </summary>
    /// <param name="itemId">The identifier of the item to load.</param>
    public async Task InitializeAsync(string itemId)
    {
        await RunCommandAsync(async () =>
        {
            ItemId = itemId;
            ImagePaths.Clear();
            ContainerId = null;
            NotifyContainerRelationStateChanged();

            var details = await itemDetailsCoordinator.GetDetailsAsync(itemId);
            if (details is null)
            {
                Name = "Item not found";
                Description = string.Empty;
                DescriptionDraft = string.Empty;
                IsEditingDescription = false;
                OnPropertyChanged(nameof(HasDescription));
                ImagePaths.Add(paths.GetFallbackImagePath());
                return;
            }

            var item = details.Inventory.Item;
            currentItem = item;
            currentAllocations = details.Inventory.Allocations;
            Name = item.Name;
            Description = item.Description;
            DescriptionDraft = item.Description;
            IsEditingDescription = false;
            ApplyQuantities(details.Inventory);
            OnPropertyChanged(nameof(HasDescription));

            ReplaceWith(ImagePaths, paths.GetItemPhotoPaths(item));

            ContainerId = details.Inventory.Allocations.FirstOrDefault()?.ContainerId.ToString();
            NotifyContainerRelationStateChanged();
        });
    }

    private void NotifyContainerRelationStateChanged()
    {
        OnPropertyChanged(nameof(HasContainerRelation));
        OnPropertyChanged(nameof(HasNoContainerRelation));
        OnPropertyChanged(nameof(HasUnassignedQuantity));
        OnPropertyChanged(nameof(ShowQuantityManagement));
        OnPropertyChanged(nameof(ShowGoToContainerButton));
    }

    [RelayCommand]
    private Task NavigateToContainerAsync()
    {
        if (currentAllocations.Count == 0) return Task.CompletedTask;

        if (currentAllocations.Count == 1)
        {
            return nav.GoToAsync(Infrastructure.NavigationRoutes.ContainerDetails,
                new Infrastructure.Navigation.ContainerDetailsNavigationRequest(currentAllocations[0].ContainerId));
        }

        if (!Guid.TryParse(ItemId, out var itemId)) return Task.CompletedTask;

        return nav.GoToAsync(Infrastructure.NavigationRoutes.ItemLocations,
            new Infrastructure.Navigation.ItemLocationsNavigationRequest(itemId));
    }

    [RelayCommand]
    private Task NavigateToAssociateWithContainerAsync()
    {
        if (!Guid.TryParse(ItemId, out var itemId)) return Task.CompletedTask;

        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.AssociateItemWithContainer,
            new Infrastructure.Navigation.AssociateItemWithContainerNavigationRequest(itemId, UnassignedQuantity));
    }

    [RelayCommand]
    private async Task EditQuantityAsync()
    {
        if (!ShowQuantityManagement || !Guid.TryParse(ItemId, out var parsedItemId))
        {
            return;
        }

        try
        {
            Guid? preferredContainerId = Guid.TryParse(sourceContainerId, out var parsedSourceContainerId)
                ? parsedSourceContainerId
                : null;
            var execution = await itemDetailsCoordinator.EditQuantityAsync(parsedItemId, preferredContainerId);
            if (execution is null)
            {
                return;
            }

            if (execution.Update.ItemDeleted)
            {
                await nav.GoBackAsync();
                return;
            }

            ApplyInventorySnapshot(execution.Inventory!);
        }
        catch (Exception ex)
        {
            await popup.ShowAlertAsync(popupDefinitions.InventoryQuantityUpdateFailed(ex.Message));
        }
    }

    [RelayCommand]
    private async Task UseAsync()
    {
        if (!ShowQuantityManagement || !Guid.TryParse(ItemId, out var parsedItemId))
        {
            return;
        }

        Guid? preferredContainerId = Guid.TryParse(sourceContainerId, out var parsedSourceContainerId)
            ? parsedSourceContainerId
            : null;

        try
        {
            var execution = await itemDetailsCoordinator.ConsumeAsync(parsedItemId, preferredContainerId);
            if (execution is null)
            {
                return;
            }

            if (execution.Update.ItemDeleted)
            {
                await nav.GoBackAsync();
                return;
            }

            ApplyInventorySnapshot(execution.Inventory!);
        }
        catch (Exception ex)
        {
            await popup.ShowAlertAsync(popupDefinitions.InventoryQuantityUpdateFailed(ex.Message));
        }
    }

    private void ApplyQuantities(InventorySnapshot inventory)
    {
        TotalQuantity = inventory.TotalQuantity;
        AssignedQuantity = inventory.AssignedQuantity;
        UnassignedQuantity = inventory.UnassignedQuantity;
    }

    private void ApplyInventorySnapshot(InventorySnapshot inventory)
    {
        currentItem = inventory.Item;
        currentAllocations = inventory.Allocations;
        ApplyQuantities(inventory);
        ContainerId = inventory.Allocations.FirstOrDefault()?.ContainerId.ToString();
        NotifyContainerRelationStateChanged();
    }

    [RelayCommand]
    private void EditDescription()
    {
        DescriptionDraft = Description;
        IsEditingDescription = true;
    }

    [RelayCommand]
    private async Task SaveDescriptionAsync()
    {
        if (currentItem is null)
        {
            return;
        }

        var updatedDescription = DescriptionDraft?.Trim() ?? string.Empty;
        await RunCommandAsync(async () =>
        {
            await itemDetailsCoordinator.UpdateDescriptionAsync(currentItem, updatedDescription);
            Description = currentItem.Description;
            DescriptionDraft = currentItem.Description;
            IsEditingDescription = false;
        });
    }

    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId)) return;

        await popup.ConfirmAndRunAsync(popupDefinitions.DeleteItem(), async () =>
        {
            await itemDetailsCoordinator.DeleteItemAsync(ItemId);
            await nav.GoBackAsync();
        });
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (currentItem is null) return;
        if (IsPhotoCaptureInProgress) return;

        var source = await SelectPhotoSourceAsync();
        if (source is null)
        {
            return;
        }

        // Run in background so persistence can finish even if the user leaves this view.
        CaptureTrackedPhotoAsync(
            operationName: "Saving item photo",
            captureAsync: progress => imageService.CaptureItemPhotoAsync(currentItem, progress, source.Value),
            targetPaths: ImagePaths,
            refreshedPaths: () => paths.GetItemPhotoPaths(currentItem)).FireAndForget(backgroundTasks, "Save item photo");
    }

    [RelayCommand]
    private Task DeletePhotoAsync()
    {
        if (currentItem is null) return Task.CompletedTask;

        return DeleteSelectedPhotoAsync(
            hasPhotos: currentItem.Photos.Count > 0,
            noPhotosPopup: popupDefinitions.NoItemPhotos(),
            pickerDefinition: popupDefinitions.ItemPhotoDeletePicker(currentItem.Photos),
            deleteAsync: imageId => imageService.DeleteItemPhotoAsync(currentItem, imageId),
            targetPaths: ImagePaths,
            refreshedPaths: () => paths.GetItemPhotoPaths(currentItem));
    }

}
