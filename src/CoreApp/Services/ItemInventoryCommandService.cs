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

    public async Task<ItemInventoryUpdateResult> SetTotalQuantityAsync(Guid itemId, int totalQuantity)
    {
        Item item = await GetItemAsync(itemId);
        item.SetTotalQuantity(totalQuantity);
        await inventoryCommands.UpdateItemAsync(item);
        return CreateResult(item, removedFromContainer: false);
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

        Item item = await GetItemAsync(itemId);
        var container = await inventoryQueries.GetContainerAsync(containerId.ToString())
            ?? throw new KeyNotFoundException($"Container '{containerId}' was not found.");
        int previousQuantity = container.Items
            .FirstOrDefault(storedItem => storedItem.ItemId == itemId)?.Quantity ?? 0;
        int resultingAssignedQuantity = item.AssignedQuantity - previousQuantity + quantity;

        if (resultingAssignedQuantity > item.TotalQuantity)
        {
            item.SetTotalQuantity(resultingAssignedQuantity);
        }

        item.SetAssignedQuantity(resultingAssignedQuantity);
        await inventoryCommands.SetItemContainerAllocationAsync(item, containerId, quantity);

        return CreateResult(item, removedFromContainer: quantity == 0);
    }

    public async Task<ItemInventoryUpdateResult> ApplyWithdrawalAsync(
        Guid itemId,
        ItemInventoryWithdrawalPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Item item = await GetItemAsync(itemId);

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

        item.SetAssignedQuantity(plan.AssignedQuantity);
        item.SetTotalQuantity(plan.TotalQuantity);
        await inventoryCommands.ApplyItemInventoryWithdrawalAsync(item, plan.Allocations);
        return CreateResult(item, removedFromContainer: false);
    }

    private async Task<Item> GetItemAsync(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
        }

        return await inventoryQueries.GetItemWithPhotosAsync(itemId.ToString())
            ?? throw new KeyNotFoundException($"Item '{itemId}' was not found.");
    }

    private static ItemInventoryUpdateResult CreateResult(Item item, bool removedFromContainer)
        => new(
            removedFromContainer,
            item.TotalQuantity,
            item.AssignedQuantity,
            item.UnassignedQuantity);
}