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
        Item item = summary.Item;
        if (totalQuantity <= item.TotalQuantity)
        {
            return CreateResult(item, summary.AssignedQuantity, removedFromContainer: false);
        }

        item.SetTotalQuantity(totalQuantity);
        await inventoryCommands.UpdateItemAsync(item);
        return CreateResult(item, summary.AssignedQuantity, removedFromContainer: false);
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
        Item item = summary.Item;
        int previousQuantity = summary.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.Quantity ?? 0;
        int resultingAssignedQuantity = summary.AssignedQuantity - previousQuantity + quantity;

        if (resultingAssignedQuantity > item.TotalQuantity)
        {
            item.SetTotalQuantity(resultingAssignedQuantity);
        }

        await inventoryCommands.SetItemContainerAllocationAsync(item, containerId, quantity);

        return CreateResult(item, resultingAssignedQuantity, removedFromContainer: quantity == 0);
    }

    public async Task<ItemInventoryUpdateResult> ApplyWithdrawalAsync(
        Guid itemId,
        ItemInventoryWithdrawalPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Item item = (await GetSummaryAsync(itemId)).Item;

        if (plan.DeleteItem)
        {
            if (plan.TotalQuantity != 0 || plan.AssignedQuantity != 0 || plan.UnassignedQuantity != 0)
            {
                throw new ArgumentException("A deletion plan must exhaust all inventory.", nameof(plan));
            }

            await inventoryCommands.DeleteItemAsync(item.ItemId.ToString());
            if (photoDeletion is not null)
            {
                await photoDeletion.DeleteItemPhotoFilesBestEffortAsync(item);
            }
            return new ItemInventoryUpdateResult(true, 0, 0, 0, ItemDeleted: true);
        }

        if (plan.TotalQuantity < 1
            || plan.AssignedQuantity < 0
            || plan.UnassignedQuantity != plan.TotalQuantity - plan.AssignedQuantity
            || plan.Allocations.Sum(allocation => allocation.Quantity) != plan.AssignedQuantity)
        {
            throw new ArgumentException("Withdrawal plan quantities are inconsistent.", nameof(plan));
        }

        item.SetTotalQuantity(plan.TotalQuantity);
        await inventoryCommands.ApplyItemInventoryWithdrawalAsync(item, plan.Allocations);
        return CreateResult(item, plan.AssignedQuantity, removedFromContainer: false);
    }

    private async Task<ItemInventorySummary> GetSummaryAsync(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
        }

        return await inventoryQueries.GetItemInventorySummaryAsync(itemId)
            ?? throw new KeyNotFoundException($"Item '{itemId}' was not found.");
    }

    private static ItemInventoryUpdateResult CreateResult(
        Item item,
        int assignedQuantity,
        bool removedFromContainer)
        => new(
            removedFromContainer,
            item.TotalQuantity,
            assignedQuantity,
            item.TotalQuantity - assignedQuantity);
}