using CoreApp.Application.Contracts.Inventory;

namespace CoreApp.Application.Features.Inventory.Allocation;

/// <summary>
/// Receives stock by composing the existing total-quantity and allocation commands.
/// </summary>
public sealed class ItemReceiptService : IItemReceiptService
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IItemInventoryCommandService inventoryCommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemReceiptService"/> class.
    /// </summary>
    /// <param name="inventoryQueries">The repository used to obtain the current item inventory.</param>
    /// <param name="inventoryCommands">The service used to persist quantity and allocation changes.</param>
    public ItemReceiptService(
        IInventoryQueryRepository inventoryQueries,
        IItemInventoryCommandService inventoryCommands)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public async Task<ItemInventoryUpdateResult> ReceiveAsync(Guid itemId, int quantity, Guid? containerId = null)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Received quantity must be positive.");
        }

        var snapshot = await inventoryQueries.GetInventorySnapshotAsync(itemId)
            ?? throw new KeyNotFoundException($"Item '{itemId}' was not found.");

        var increasedInventory = await inventoryCommands.IncreaseTotalQuantityAsync(itemId, snapshot.TotalQuantity + quantity);
        if (containerId is not { } destinationContainerId || destinationContainerId == Guid.Empty)
        {
            return increasedInventory;
        }

        var existingDestinationQuantity = snapshot.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == destinationContainerId)?.Quantity ?? 0;

        return await inventoryCommands.SetContainerAllocationAsync(
            itemId,
            destinationContainerId,
            existingDestinationQuantity + quantity);
    }
}