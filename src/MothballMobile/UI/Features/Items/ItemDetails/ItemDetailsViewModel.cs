using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Entities.ItemAggregate;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using CoreApp.Services;
using Microsoft.Extensions.Logging;

namespace MothballMobile.UI.Features.Items.ItemDetails;

public partial class ItemDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable
{
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IItemInventoryCommandService inventoryCommands;
    private readonly IDeleteItemCommandHandler deleteItemHandler;
    private readonly INavigationService nav;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly ILogger<ItemDetailsViewModel> logger;
    private Item? currentItem;
    private IReadOnlyList<CoreApp.Contracts.ItemContainerAllocation> currentAllocations = [];
    private string? sourceContainerId;

    [ObservableProperty]
    private string itemId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

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
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool ShowGoToContainerButton => HasContainerRelation
        && (string.IsNullOrWhiteSpace(sourceContainerId)
            || !string.Equals(ContainerId, sourceContainerId, StringComparison.OrdinalIgnoreCase));

    public ObservableCollection<string> ImagePaths { get; } = new();

    public ItemDetailsViewModel(
        IItemDetailsQueryHandler itemDetailsQueries,
        IItemInventoryCommandService inventoryCommands,
        IDeleteItemCommandHandler deleteItemHandler,
        INavigationService nav,
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
        this.nav = nav;
        this.backgroundTasks = backgroundTasks;
        this.logger = logger;
    }

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

    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            return Task.CompletedTask;
        }

        return InitializeAsync(ItemId);
    }

    public async Task InitializeAsync(string itemId)
    {
        await RunCommandAsync(async () =>
        {
            ItemId = itemId;
            ImagePaths.Clear();
            ContainerId = null;
            TotalQuantity = 0;
            AssignedQuantity = 0;
            UnassignedQuantity = 0;
            NotifyContainerRelationStateChanged();

            var details = await itemDetailsQueries.GetDetailsAsync(itemId);
            if (details is null)
            {
                Name = "Item not found";
                Description = string.Empty;
                OnPropertyChanged(nameof(HasDescription));
                ImagePaths.Add(paths.GetFallbackImagePath());
                return;
            }

            var item = details.Item;
            currentItem = item;
            currentAllocations = details.Allocations ?? [];
            Name = item.Name;
            Description = item.Description;
            TotalQuantity = item.TotalQuantity;
            AssignedQuantity = item.AssignedQuantity;
            UnassignedQuantity = item.UnassignedQuantity;
            OnPropertyChanged(nameof(HasDescription));

            ReplaceWith(ImagePaths, paths.GetItemPhotoPaths(item));

            ContainerId = details.ContainerId?.ToString();
            NotifyContainerRelationStateChanged();
        });
    }

    private void NotifyContainerRelationStateChanged()
    {
        OnPropertyChanged(nameof(HasContainerRelation));
        OnPropertyChanged(nameof(HasNoContainerRelation));
        OnPropertyChanged(nameof(ShowGoToContainerButton));
    }

    [RelayCommand]
    private Task NavigateToContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return Task.CompletedTask;
        return nav.GoToAsync(Infrastructure.NavigationRoutes.ContainerDetails,
            new Dictionary<string, object> { [Infrastructure.NavigationParams.ContainerId] = ContainerId! });
    }

    [RelayCommand]
    private Task NavigateToAssociateWithContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId)) return Task.CompletedTask;

        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.AssociateItemWithContainer,
            new Dictionary<string, object> { [NavigationParams.ItemId] = ItemId });
    }

    [RelayCommand]
    private async Task SetTotalQuantityAsync()
    {
        if (!await RefreshInventoryForQuantityEditAsync())
        {
            return;
        }

        var selectedQuantity = await popup.PickNumberAsync(
            popupDefinitions.SetTotalQuantity(TotalQuantity, AssignedQuantity));

        if (selectedQuantity is null || selectedQuantity.Value == TotalQuantity)
        {
            return;
        }

        try
        {
            logger.LogInformation(
                "Set item total requested: itemId={ItemId}, selected={Selected}, currentTotal={CurrentTotal}, assigned={Assigned}, sourceContainer={SourceContainer}",
                currentItem!.ItemId,
                selectedQuantity.Value,
                TotalQuantity,
                AssignedQuantity,
                sourceContainerId);

            if (selectedQuantity.Value == 0)
            {
                if (!await popup.ConfirmAsync(
                    popupDefinitions.DeleteItemBySettingTotalToZero(Name)))
                {
                    return;
                }

                var deletionPlan = new CoreApp.Contracts.ItemInventoryWithdrawalPlan(0, 0, 0, [], true);
                var deletionResult = await inventoryCommands.ApplyWithdrawalAsync(currentItem.ItemId, deletionPlan);
                if (deletionResult.ItemDeleted)
                {
                    await nav.GoBackAsync();
                }

                return;
            }

            if (selectedQuantity.Value > TotalQuantity)
            {
                logger.LogDebug("Routing item total request to increase command.");
                var result = await inventoryCommands.IncreaseTotalQuantityAsync(currentItem.ItemId, selectedQuantity.Value);
                ApplyInventoryResult(result);
                return;
            }

            logger.LogDebug("Routing item total request to withdrawal workflow.");
            await RunWithdrawalWorkflowAsync(selectedQuantity.Value);
        }
        catch (Exception ex)
        {
            await popup.ShowAlertAsync(popupDefinitions.InventoryQuantityUpdateFailed(ex.Message));
        }
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

        currentItem = details.Item;
        currentAllocations = details.Allocations ?? [];
        TotalQuantity = details.Item.TotalQuantity;
        AssignedQuantity = details.Item.AssignedQuantity;
        UnassignedQuantity = details.Item.UnassignedQuantity;
        return true;
    }

    private async Task RunWithdrawalWorkflowAsync(int requestedTotal)
    {
        var originalAllocations = currentAllocations
            .Where(allocation => allocation.Quantity > 0)
            .ToList();
        var remainingAllocations = originalAllocations.ToDictionary(
            allocation => allocation.ContainerId,
            allocation => allocation);
        var withdrawals = new List<CoreApp.Contracts.ItemAllocationWithdrawal>();
        int assignedRemaining = remainingAllocations.Values.Sum(allocation => allocation.Quantity);
        int assignedToWithdraw = ItemInventoryWithdrawalPlanner.GetRequiredAssignedWithdrawal(
            TotalQuantity,
            requestedTotal,
            assignedRemaining);
        int assignedWithdrawn = 0;
        int carriedQuantity = 0;
        Guid? preferredContainerId = Guid.TryParse(sourceContainerId, out var parsedSourceContainerId)
            ? parsedSourceContainerId
            : null;
        bool usePreferredContainer = true;

        while (assignedWithdrawn < assignedToWithdraw || carriedQuantity > 0)
        {
            var choices = remainingAllocations.Values
                .Where(allocation => allocation.Quantity > 0)
                .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (choices.Count == 0)
            {
                return;
            }

            CoreApp.Contracts.ItemContainerAllocation? selectedContainer = null;
            if (usePreferredContainer)
            {
                selectedContainer = ItemInventoryWithdrawalPlanner.GetPreferredAllocation(
                    choices,
                    preferredContainerId);
                usePreferredContainer = false;
            }

            selectedContainer ??= await popup.SelectOptionAsync(
                popupDefinitions.WithdrawalContainerPicker(choices));
            if (selectedContainer is null)
            {
                return;
            }

            var selectedWithdrawal = await popup.PickNumberAsync(
                popupDefinitions.WithdrawFromContainer(
                    selectedContainer,
                    carriedQuantity,
                    assignedToWithdraw - assignedWithdrawn));
            if (selectedWithdrawal is null || selectedWithdrawal.Value == 0)
            {
                return;
            }

            if (selectedWithdrawal.Value < carriedQuantity)
            {
                await popup.ShowAlertAsync(popupDefinitions.WithdrawalCarryTooSmall(carriedQuantity));
                continue;
            }

            int removed = Math.Min(selectedContainer.Quantity, selectedWithdrawal.Value);
            assignedRemaining -= removed;
            assignedWithdrawn += removed;
            carriedQuantity = selectedWithdrawal.Value - removed;
            remainingAllocations[selectedContainer.ContainerId] = selectedContainer with
            {
                Quantity = selectedContainer.Quantity - removed,
            };
            withdrawals.Add(new CoreApp.Contracts.ItemAllocationWithdrawal(
                selectedContainer.ContainerId,
                selectedWithdrawal.Value));
        }

        var unassignedWithdrawals = new List<int>();
        var plan = ItemInventoryWithdrawalPlanner.Plan(
            TotalQuantity,
            originalAllocations,
            withdrawals,
            unassignedWithdrawals,
            requestedTotal);

        if (plan.UnassignedQuantity > 0
            && await popup.ConfirmAsync(
                popupDefinitions.ConfirmUnassignedWithdrawal(plan.UnassignedQuantity)))
        {
            while (plan.UnassignedQuantity > 0)
            {
                var selectedWithdrawal = await popup.PickNumberAsync(
                    popupDefinitions.WithdrawUnassignedQuantity(plan.UnassignedQuantity));
                if (selectedWithdrawal is null || selectedWithdrawal.Value == 0)
                {
                    break;
                }

                unassignedWithdrawals.Add(selectedWithdrawal.Value);
                plan = ItemInventoryWithdrawalPlanner.Plan(
                    TotalQuantity,
                    originalAllocations,
                    withdrawals,
                    unassignedWithdrawals,
                    requestedTotal);
            }
        }

        var result = await inventoryCommands.ApplyWithdrawalAsync(currentItem!.ItemId, plan);
        if (result.ItemDeleted)
        {
            await nav.GoBackAsync();
            return;
        }

        currentAllocations = plan.Allocations;
        ApplyInventoryResult(result);
    }

    private void ApplyInventoryResult(CoreApp.Contracts.ItemInventoryUpdateResult result)
    {
        currentItem!.ApplyInventoryQuantities(result.TotalQuantity, result.AssignedQuantity);
        TotalQuantity = result.TotalQuantity;
        AssignedQuantity = result.AssignedQuantity;
        UnassignedQuantity = result.UnassignedQuantity;
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
