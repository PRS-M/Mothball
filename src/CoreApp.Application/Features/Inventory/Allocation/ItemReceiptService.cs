using CoreApp.Domain.Entities.InventoryAggregate;

namespace CoreApp.Application.Features.Inventory.Allocation;

/// <summary>
/// Receives stock by composing the existing total-quantity and allocation commands.
/// </summary>
public sealed class ItemReceiptService : IItemReceiptService
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IItemInventoryCommandService inventoryCommands;
    private readonly CanonicalInventoryCommandService? canonicalCommands;
    private readonly IWorkspaceContext? workspaceContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemReceiptService"/> class.
    /// </summary>
    /// <param name="inventoryQueries">The repository used to obtain the current item inventory.</param>
    /// <param name="inventoryCommands">The service used to persist quantity and allocation changes.</param>
    public ItemReceiptService(
        IInventoryQueryRepository inventoryQueries,
        IItemInventoryCommandService inventoryCommands,
        CanonicalInventoryCommandService? canonicalCommands = null,
        IWorkspaceContext? workspaceContext = null)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.canonicalCommands = canonicalCommands;
        this.workspaceContext = workspaceContext;
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

        if (canonicalCommands is not null && workspaceContext is not null)
        {
            var defaults = (await workspaceContext.EnsureDefaultAsync()).Defaults;
            var workspaceId = new InventoryWorkspaceId(defaults.WorkspaceId);
            await SeedLegacyBalancesAsync(snapshot, workspaceId, defaults.UnassignedLocationId);
            var destination = containerId is { } id && id != Guid.Empty ? id : defaults.UnassignedLocationId;
            await canonicalCommands.ReceiveAsync(workspaceId, itemId, new InventoryPlacementId(destination), quantity, "Personal Storage receipt", Guid.NewGuid());
            var assigned = snapshot.AssignedQuantity + (destination == defaults.UnassignedLocationId ? 0 : quantity);
            var total = snapshot.TotalQuantity + quantity;
            return new ItemInventoryUpdateResult(false, total, assigned, total - assigned);
        }

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

    private async Task SeedLegacyBalancesAsync(InventorySnapshot snapshot, InventoryWorkspaceId workspaceId, Guid unassignedLocationId)
    {
        foreach (var allocation in snapshot.Allocations)
            await canonicalCommands!.EnsureOpeningBalanceAsync(workspaceId, snapshot.Item.ItemId, new InventoryPlacementId(allocation.ContainerId), allocation.Quantity);
        await canonicalCommands!.EnsureOpeningBalanceAsync(workspaceId, snapshot.Item.ItemId, new InventoryPlacementId(unassignedLocationId), snapshot.UnassignedQuantity);
    }
}
