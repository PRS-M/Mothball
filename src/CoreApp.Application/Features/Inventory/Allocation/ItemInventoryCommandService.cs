using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Features.Photos;
using CoreApp.Domain.Inventory;

namespace CoreApp.Application.Features.Inventory.Allocation;

public sealed class ItemInventoryCommandService : IItemInventoryCommandService
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IPhotoDeletionService? photoDeletion;
    private readonly CanonicalInventoryCommandService? canonicalCommands;
    private readonly IWorkspaceContext? workspaceContext;

    public ItemInventoryCommandService(
        IInventoryQueryRepository inventoryQueries,
        IInventoryCommandRepository inventoryCommands,
        IPhotoDeletionService? photoDeletion = null,
        CanonicalInventoryCommandService? canonicalCommands = null,
        IWorkspaceContext? workspaceContext = null)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.photoDeletion = photoDeletion;
        this.canonicalCommands = canonicalCommands;
        this.workspaceContext = workspaceContext;
    }

    /// <inheritdoc />
    public async Task<ItemInventoryUpdateResult> IncreaseTotalQuantityAsync(Guid itemId, int totalQuantity)
    {
        var summary = await GetSummaryAsync(itemId);
        var inventory = ToInventory(summary);
        if (totalQuantity <= inventory.TotalQuantity)
        {
            return CreateResult(inventory, removedFromContainer: false);
        }

        if (canonicalCommands is not null && workspaceContext is not null)
        {
            var context = await workspaceContext.EnsureDefaultAsync();
            var workspaceId = new InventoryWorkspaceId(context.Workspace.WorkspaceId);
            await SeedLegacyBalancesAsync(summary, workspaceId, context.Defaults.UnassignedLocationId);
            await canonicalCommands.AdjustAsync(
                workspaceId,
                itemId,
                new InventoryPlacementId(context.Defaults.UnassignedLocationId),
                totalQuantity - inventory.TotalQuantity,
                "Personal Storage quantity increase",
                Guid.NewGuid());
            return new ItemInventoryUpdateResult(false, totalQuantity, inventory.AssignedQuantity, totalQuantity - inventory.AssignedQuantity);
        }

        inventory.IncreaseTotalQuantity(totalQuantity);
        await inventoryCommands.SaveItemInventoryAsync(inventory);
        return CreateResult(inventory, removedFromContainer: false);
    }

    /// <inheritdoc />
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

        if (canonicalCommands is not null && workspaceContext is not null)
        {
            var context = await workspaceContext.EnsureDefaultAsync();
            var workspaceId = new InventoryWorkspaceId(context.Workspace.WorkspaceId);
            await SeedLegacyBalancesAsync(summary, workspaceId, context.Defaults.UnassignedLocationId);
            var oldQuantity = summary.Allocations.FirstOrDefault(x => x.ContainerId == containerId)?.Quantity ?? 0;
            var newAssignedQuantity = summary.AssignedQuantity - oldQuantity + quantity;
            var newTotalQuantity = Math.Max(summary.TotalQuantity, newAssignedQuantity);
            var unassigned = new InventoryPlacementId(context.Defaults.UnassignedLocationId);
            if (newTotalQuantity > summary.TotalQuantity)
            {
                await canonicalCommands.ReceiveAsync(workspaceId, itemId, unassigned, newTotalQuantity - summary.TotalQuantity, "Personal Storage allocation increase", Guid.NewGuid());
            }

            var delta = quantity - oldQuantity;
            if (delta > 0)
            {
                await canonicalCommands.TransferAsync(workspaceId, itemId, unassigned, new InventoryPlacementId(containerId), delta, "Personal Storage allocation", Guid.NewGuid());
            }
            else if (delta < 0)
            {
                await canonicalCommands.TransferAsync(workspaceId, itemId, new InventoryPlacementId(containerId), unassigned, -delta, "Personal Storage allocation reduction", Guid.NewGuid());
            }

            return new ItemInventoryUpdateResult(quantity == 0, newTotalQuantity, newAssignedQuantity, newTotalQuantity - newAssignedQuantity);
        }

        inventory.SetContainerAllocation(containerId, containerName, quantity);

        await inventoryCommands.SaveItemInventoryAsync(inventory);

        return CreateResult(inventory, removedFromContainer: quantity == 0);
    }

    /// <inheritdoc />
    public async Task<ItemInventoryUpdateResult> ConsumeAsync(
        Guid itemId,
        ItemInventoryConsumptionSource source,
        int quantity)
    {
        var summary = await GetSummaryAsync(itemId);
        var plan = ItemInventoryConsumptionPlanner.Plan(summary, source, quantity);

        if (canonicalCommands is not null && workspaceContext is not null)
        {
            var context = await workspaceContext.EnsureDefaultAsync();
            var workspaceId = new InventoryWorkspaceId(context.Workspace.WorkspaceId);
            await SeedLegacyBalancesAsync(summary, workspaceId, context.Defaults.UnassignedLocationId);
            var placementId = source.Kind == ItemInventoryConsumptionSourceKind.Unassigned
                ? context.Defaults.UnassignedLocationId
                : source.ContainerId!.Value;
            await canonicalCommands.WithdrawAsync(workspaceId, itemId, new InventoryPlacementId(placementId), quantity, "Personal Storage withdrawal", Guid.NewGuid());

            if (plan.DeleteItem)
            {
                await inventoryCommands.DeleteItemAsync(summary.Item.ItemId.ToString());
                if (photoDeletion is not null)
                    await photoDeletion.DeleteItemPhotoFilesBestEffortAsync(summary.Item);
                return new ItemInventoryUpdateResult(true, 0, 0, 0, ItemDeleted: true);
            }

            return new ItemInventoryUpdateResult(false, plan.TotalQuantity, plan.AssignedQuantity, plan.UnassignedQuantity);
        }

        return await ApplyWithdrawalAsync(summary, plan);
    }

    /// <inheritdoc />
    public async Task<ItemInventoryUpdateResult> ApplyWithdrawalAsync(
        Guid itemId,
        ItemInventoryWithdrawalPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var summary = await GetSummaryAsync(itemId);
        return await ApplyWithdrawalAsync(summary, plan);
    }

    private async Task<ItemInventoryUpdateResult> ApplyWithdrawalAsync(
        InventorySnapshot summary,
        ItemInventoryWithdrawalPlan plan)
    {
        var inventory = ToInventory(summary);

        if (canonicalCommands is not null && workspaceContext is not null)
        {
            var context = await workspaceContext.EnsureDefaultAsync();
            var workspaceId = new InventoryWorkspaceId(context.Workspace.WorkspaceId);
            await SeedLegacyBalancesAsync(summary, workspaceId, context.Defaults.UnassignedLocationId);
            var targetByPlacement = plan.Allocations.ToDictionary(x => x.ContainerId, x => x.Quantity);
            foreach (var allocation in summary.Allocations)
            {
                var targetQuantity = targetByPlacement.GetValueOrDefault(allocation.ContainerId);
                if (allocation.Quantity > targetQuantity)
                {
                    await canonicalCommands.WithdrawAsync(workspaceId, summary.Item.ItemId, new InventoryPlacementId(allocation.ContainerId), allocation.Quantity - targetQuantity, "Personal Storage withdrawal", Guid.NewGuid());
                }
            }

            if (summary.UnassignedQuantity > plan.UnassignedQuantity)
            {
                await canonicalCommands.WithdrawAsync(workspaceId, summary.Item.ItemId, new InventoryPlacementId(context.Defaults.UnassignedLocationId), summary.UnassignedQuantity - plan.UnassignedQuantity, "Personal Storage withdrawal", Guid.NewGuid());
            }

            if (plan.DeleteItem)
            {
                await inventoryCommands.DeleteItemAsync(summary.Item.ItemId.ToString());
                if (photoDeletion is not null)
                    await photoDeletion.DeleteItemPhotoFilesBestEffortAsync(summary.Item);
                return new ItemInventoryUpdateResult(true, 0, 0, 0, ItemDeleted: true);
            }

            return new ItemInventoryUpdateResult(false, plan.TotalQuantity, plan.AssignedQuantity, plan.UnassignedQuantity);
        }

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

    private async Task SeedLegacyBalancesAsync(InventorySnapshot summary, InventoryWorkspaceId workspaceId, Guid unassignedLocationId)
    {
        foreach (var allocation in summary.Allocations)
            await canonicalCommands!.EnsureOpeningBalanceAsync(workspaceId, summary.Item.ItemId, new InventoryPlacementId(allocation.ContainerId), allocation.Quantity);
        await canonicalCommands!.EnsureOpeningBalanceAsync(workspaceId, summary.Item.ItemId, new InventoryPlacementId(unassignedLocationId), summary.UnassignedQuantity);
    }
}
