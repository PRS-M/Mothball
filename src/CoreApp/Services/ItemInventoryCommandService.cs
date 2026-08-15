using CoreApp.Entities.Inventory;
using CoreApp.Contracts;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class ItemInventoryCommandService : IItemInventoryCommandService
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IPhotoDeletionService? photoDeletion;

    public ItemInventoryCommandService(
        IInventoryQueryRepository inventoryQueries,
        IInventoryCommandRepository inventoryCommands,
        IPhotoDeletionService? photoDeletion = null)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.photoDeletion = photoDeletion;
    }

    public async Task<ItemInventoryUpdateResult> IncreaseTotalQuantityAsync(Guid itemId, int totalQuantity)
    {
        var summary = await GetSummaryAsync(itemId);
        var inventory = ToInventory(summary);
        if (totalQuantity <= inventory.TotalQuantity)
        {
            return CreateResult(inventory, removedFromContainer: false);
        }

        inventory.IncreaseTotalQuantity(totalQuantity);
        await inventoryCommands.SaveItemInventoryAsync(inventory);
        return CreateResult(inventory, removedFromContainer: false);
    }

    public async Task<ItemInventoryUpdateResult> SetContainerAllocationAsync(
        Guid itemId,
        Guid containerId,
        int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Allocated quantity cannot be negative.");
        }

        var summary = await GetSummaryAsync(itemId);
        var inventory = ToInventory(summary);
        string containerName = summary.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.ContainerName ?? string.Empty;
        inventory.SetContainerAllocation(containerId, containerName, quantity);

        await inventoryCommands.SaveItemInventoryAsync(inventory);

        return CreateResult(inventory, removedFromContainer: quantity == 0);
    }

    public async Task<ItemInventoryUpdateResult> ApplyWithdrawalAsync(
        Guid itemId,
        ItemInventoryWithdrawalPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var summary = await GetSummaryAsync(itemId);
        var inventory = ToInventory(summary);

        if (plan.DeleteItem)
        {
            inventory.ApplyWithdrawal(plan);
            await inventoryCommands.DeleteItemAsync(summary.Item.ItemId.ToString());
            if (photoDeletion is not null)
            {
                await photoDeletion.DeleteItemPhotoFilesBestEffortAsync(summary.Item);
            }
            return new ItemInventoryUpdateResult(true, 0, 0, 0, ItemDeleted: true);
        }

        inventory.ApplyWithdrawal(plan);
        await inventoryCommands.SaveItemInventoryAsync(inventory);
        return CreateResult(inventory, removedFromContainer: false);
    }

    private async Task<InventorySnapshot> GetSummaryAsync(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
        }

        return await inventoryQueries.GetInventorySnapshotAsync(itemId)
            ?? throw new KeyNotFoundException($"Item '{itemId}' was not found.");
    }

    private static ItemInventory ToInventory(InventorySnapshot snapshot)
        => new(snapshot.Item.ItemId, snapshot.TotalQuantity, snapshot.Allocations);

    private static ItemInventoryUpdateResult CreateResult(
        ItemInventory inventory,
        bool removedFromContainer)
        => new(
            removedFromContainer,
            inventory.TotalQuantity,
            inventory.AssignedQuantity,
            inventory.UnassignedQuantity);
}
