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
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IItemInventoryCommandService inventoryCommands;
    private readonly IDeleteItemCommandHandler deleteItemHandler;
    private readonly IUpdateItemDescriptionCommandHandler updateItemDescriptionHandler;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly ILogger<ItemDetailsViewModel> logger;
    private Item? currentItem;
    private InventorySnapshot? currentInventory;
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
        IItemDetailsQueryHandler itemDetailsQueries,
        IItemInventoryCommandService inventoryCommands,
        IDeleteItemCommandHandler deleteItemHandler,
        IUpdateItemDescriptionCommandHandler updateItemDescriptionHandler,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IImagePathResolver paths,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ImageService imageService,
        IPhotoBackgroundOperationTracker photoBackgroundOperationTracker,
        IBackgroundTaskObserver backgroundTasks,
        ILogger<ItemDetailsViewModel> logger)
        : base(paths, imageService, popup, popupDefinitions, photoBackgroundOperationTracker)
    {
        this.itemDetailsQueries = itemDetailsQueries;
        this.inventoryCommands = inventoryCommands;
        this.deleteItemHandler = deleteItemHandler;
        this.updateItemDescriptionHandler = updateItemDescriptionHandler;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.backgroundTasks = backgroundTasks;
        this.logger = logger;
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

            var details = await itemDetailsQueries.GetDetailsAsync(itemId);
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
            currentInventory = details.Inventory;
            currentAllocations = details.Inventory.Allocations;
            Name = item.Name;
            Description = item.Description;
            DescriptionDraft = item.Description;
            IsEditingDescription = false;
            TotalQuantity = details.Inventory.TotalQuantity;
            AssignedQuantity = details.Inventory.AssignedQuantity;
            UnassignedQuantity = details.Inventory.UnassignedQuantity;
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
                new Dictionary<string, object>
                {
                    [Infrastructure.NavigationParams.ContainerId] = currentAllocations[0].ContainerId.ToString()
                });
        }

        if (string.IsNullOrWhiteSpace(ItemId)) return Task.CompletedTask;

        return nav.GoToAsync(Infrastructure.NavigationRoutes.ItemLocations,
            new Dictionary<string, object> { [NavigationParams.ItemId] = ItemId });
    }

    [RelayCommand]
    private Task NavigateToAssociateWithContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId)) return Task.CompletedTask;

        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.AssociateItemWithContainer,
            new Dictionary<string, object>
            {
                [NavigationParams.ItemId] = ItemId,
                [NavigationParams.UnassignedQuantity] = UnassignedQuantity,
            });
    }

    [RelayCommand]
    private async Task SetTotalQuantityAsync()
    {
        if (!ShowQuantityManagement)
        {
            return;
        }

        if (!await RefreshInventoryForQuantityEditAsync())
        {
            return;
        }

        var snapshot = new QuantityEditSnapshot(currentItem!, currentInventory!);

        var selectedQuantity = await popup.PickNumberAsync(
            popupDefinitions.SetTotalQuantity(snapshot.TotalQuantity, snapshot.AssignedQuantity));

        if (selectedQuantity is null || selectedQuantity.Value == snapshot.TotalQuantity)
        {
            return;
        }

        try
        {
            await ApplyTotalQuantitySelectionAsync(snapshot, selectedQuantity.Value);
        }
        catch (Exception ex)
        {
            await popup.ShowAlertAsync(popupDefinitions.InventoryQuantityUpdateFailed(ex.Message));
        }
    }

    private async Task ApplyTotalQuantitySelectionAsync(QuantityEditSnapshot snapshot, int selectedQuantity)
    {
        logger.LogInformation(
            "Set item total requested: itemId={ItemId}, selected={Selected}, currentTotal={CurrentTotal}, assigned={Assigned}, sourceContainer={SourceContainer}",
            snapshot.Item.ItemId,
            selectedQuantity,
            snapshot.TotalQuantity,
            snapshot.AssignedQuantity,
            sourceContainerId);

        if (selectedQuantity == 0)
        {
            await DeleteBySettingTotalToZeroAsync(snapshot.Item);
            return;
        }

        if (selectedQuantity > snapshot.TotalQuantity)
        {
            await IncreaseTotalQuantityAsync(snapshot.Item, selectedQuantity);
            return;
        }

        logger.LogDebug("Routing item total request to withdrawal workflow.");
        await RunWithdrawalWorkflowAsync(selectedQuantity, snapshot.Inventory, snapshot.Item);
    }

    private async Task DeleteBySettingTotalToZeroAsync(Item item)
    {
        if (!await popup.ConfirmAsync(popupDefinitions.DeleteItemBySettingTotalToZero(Name)))
        {
            return;
        }

        var deletionPlan = new ItemInventoryWithdrawalPlan(0, 0, 0, [], true);
        var deletionResult = await inventoryCommands.ApplyWithdrawalAsync(item.ItemId, deletionPlan);
        if (deletionResult.ItemDeleted)
        {
            await nav.GoBackAsync();
        }
    }

    private async Task IncreaseTotalQuantityAsync(Item item, int selectedQuantity)
    {
        logger.LogDebug("Routing item total request to increase command.");
        var result = await inventoryCommands.IncreaseTotalQuantityAsync(item.ItemId, selectedQuantity);
        ApplyInventoryResult(result);
    }

    private async Task<bool> RefreshInventoryForQuantityEditAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            return false;
        }

        var details = await itemDetailsQueries.GetDetailsAsync(ItemId);
        if (details is null)
        {
            return false;
        }

        currentItem = details.Inventory.Item;
        currentInventory = details.Inventory;
        currentAllocations = details.Inventory.Allocations;
        TotalQuantity = details.Inventory.TotalQuantity;
        AssignedQuantity = details.Inventory.AssignedQuantity;
        UnassignedQuantity = details.Inventory.UnassignedQuantity;
        ContainerId = details.Inventory.Allocations.FirstOrDefault()?.ContainerId.ToString();
        NotifyContainerRelationStateChanged();
        return true;
    }

    private async Task RunWithdrawalWorkflowAsync(
        int requestedTotal,
        InventorySnapshot inventorySnapshot,
        Item itemSnapshot)
    {
        Guid? preferredContainerId = Guid.TryParse(sourceContainerId, out var parsedSourceContainerId)
            ? parsedSourceContainerId
            : null;
        var session = new ItemInventoryAdjustmentSession(
            inventorySnapshot,
            requestedTotal,
            preferredContainerId);

        while (true)
        {
            switch (session.State)
            {
                case ItemInventoryAdjustmentState.WithdrawAssigned:
                    var selectedContainer = session.PreferredAllocation
                        ?? await popup.SelectOptionAsync(
                            popupDefinitions.WithdrawalContainerPicker(session.RemainingAllocations));
                    if (selectedContainer is null)
                    {
                        session.Cancel();
                        continue;
                    }

                    var assignedWithdrawal = await popup.PickNumberAsync(
                        popupDefinitions.WithdrawFromContainer(
                            selectedContainer,
                            session.CarriedWithdrawal,
                            session.SuggestedAssignedWithdrawal));
                    if (assignedWithdrawal is null)
                    {
                        session.Cancel();
                        continue;
                    }

                    try
                    {
                        session.WithdrawAssigned(selectedContainer.ContainerId, assignedWithdrawal.Value);
                    }
                    catch (InvalidOperationException)
                    {
                        await popup.ShowAlertAsync(
                            popupDefinitions.WithdrawalCarryTooSmall(session.CarriedWithdrawal));
                    }
                    break;

                case ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal:
                    if (await popup.ConfirmAsync(
                        popupDefinitions.ConfirmUnassignedWithdrawal(session.UnassignedQuantity)))
                    {
                        session.AcceptUnassignedWithdrawal();
                    }
                    else
                    {
                        session.DeclineUnassignedWithdrawal();
                    }
                    break;

                case ItemInventoryAdjustmentState.WithdrawUnassigned:
                    var unassignedWithdrawal = await popup.PickNumberAsync(
                        popupDefinitions.WithdrawUnassignedQuantity(session.UnassignedQuantity));
                    session.WithdrawUnassigned(unassignedWithdrawal ?? 0);
                    break;

                case ItemInventoryAdjustmentState.ReadyToCommit:
                    var plan = session.BuildPlan();
                    var result = await inventoryCommands.ApplyWithdrawalAsync(itemSnapshot.ItemId, plan);
                    if (result.ItemDeleted)
                    {
                        await nav.GoBackAsync();
                        return;
                    }

                    currentAllocations = plan.Allocations;
                    ApplyInventoryResult(result);
                    return;

                case ItemInventoryAdjustmentState.Cancelled:
                    return;

                default:
                    throw new InvalidOperationException($"Unsupported adjustment state {session.State}.");
            }
        }
    }

    private void ApplyInventoryResult(ItemInventoryUpdateResult result)
    {
        currentInventory = new InventorySnapshot(
            currentItem!,
            result.TotalQuantity,
            result.AssignedQuantity,
            currentAllocations);
        TotalQuantity = result.TotalQuantity;
        AssignedQuantity = result.AssignedQuantity;
        UnassignedQuantity = result.UnassignedQuantity;
        ContainerId = currentAllocations.FirstOrDefault()?.ContainerId.ToString();
        NotifyContainerRelationStateChanged();
    }

    private sealed record QuantityEditSnapshot(Item Item, InventorySnapshot Inventory)
    {
        public int TotalQuantity => Inventory.TotalQuantity;
        public int AssignedQuantity => Inventory.AssignedQuantity;
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
            await updateItemDescriptionHandler.UpdateAsync(currentItem, updatedDescription);
            Description = currentItem.Description;
            DescriptionDraft = currentItem.Description;
            IsEditingDescription = false;
        });
    }

    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId)) return;
        var confirmed = await popup.ConfirmAsync(popupDefinitions.DeleteItem());
        if (!confirmed) return;

        await deleteItemHandler.DeleteAsync(ItemId);
        await nav.GoBackAsync();
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
    private async Task DeletePhotoAsync()
    {
        if (currentItem is null) return;
        if (currentItem.Photos.Count == 0)
        {
            await popup.ShowAlertAsync(popupDefinitions.NoItemPhotos());
            return;
        }

        var selectedPhoto = await SelectPhotoAsync(popupDefinitions.ItemPhotoDeletePicker(currentItem.Photos));
        if (selectedPhoto is null)
        {
            return;
        }

        var confirmed = await popup.ConfirmAsync(popupDefinitions.DeletePhoto());

        if (!confirmed)
        {
            return;
        }

        await RunCommandAsync(async () =>
        {
            var deleted = await imageService.DeleteItemPhotoAsync(currentItem, selectedPhoto.ImageId);
            if (deleted)
            {
                ReplaceWith(ImagePaths, paths.GetItemPhotoPaths(currentItem));
            }
        });
    }

}
