using System.Text.Json;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;

namespace CoreApp.Application.Features.Inventory;

/// <summary>Application command seam for canonical inventory mutations.</summary>
public sealed class CanonicalInventoryCommandService(ICanonicalInventoryMutationStore inventory)
{
    /// <summary>Seeds a missing canonical placement from legacy inventory during compatibility migration.</summary>
    public async Task EnsureOpeningBalanceAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, int quantity, Guid? operationId = null, CancellationToken cancellationToken = default)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (await inventory.GetBalanceAsync(workspaceId, itemId, placementId, cancellationToken).ConfigureAwait(false) is not null) return;
        if (quantity == 0) return;
        await AdjustAsync(workspaceId, itemId, placementId, quantity, "Legacy opening balance", operationId ?? Guid.NewGuid(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Receives stock into a placement and queues the same mutation for synchronization.</summary>
    public async Task<InventoryMovementPlan> ReceiveAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, int quantity, string reason, Guid operationId, CancellationToken cancellationToken = default)
    {
        var balance = await GetOrCreateAsync(workspaceId, itemId, placementId, cancellationToken).ConfigureAwait(false);
        return await ApplyAsync(InventoryMovementPlanner.PlanReceipt(balance, quantity, reason, DateTimeOffset.UtcNow, operationId), operationId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Withdraws stock from a placement and queues the same mutation for synchronization.</summary>
    public async Task<InventoryMovementPlan> WithdrawAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, int quantity, string reason, Guid operationId, CancellationToken cancellationToken = default)
    {
        var balance = await RequireBalanceAsync(workspaceId, itemId, placementId, cancellationToken).ConfigureAwait(false);
        return await ApplyAsync(InventoryMovementPlanner.PlanWithdrawal(balance, quantity, reason, DateTimeOffset.UtcNow, operationId), operationId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Transfers stock between placements and queues the same mutation for synchronization.</summary>
    public async Task<InventoryMovementPlan> TransferAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId source, InventoryPlacementId destination, int quantity, string reason, Guid operationId, CancellationToken cancellationToken = default)
    {
        var sourceBalance = await RequireBalanceAsync(workspaceId, itemId, source, cancellationToken).ConfigureAwait(false);
        var destinationBalance = await GetOrCreateAsync(workspaceId, itemId, destination, cancellationToken).ConfigureAwait(false);
        return await ApplyAsync(InventoryMovementPlanner.PlanTransfer(sourceBalance, destinationBalance, quantity, reason, DateTimeOffset.UtcNow, operationId), operationId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Applies a signed stock adjustment and queues the same mutation for synchronization.</summary>
    public async Task<InventoryMovementPlan> AdjustAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, int delta, string reason, Guid operationId, CancellationToken cancellationToken = default)
    {
        var balance = await GetOrCreateAsync(workspaceId, itemId, placementId, cancellationToken).ConfigureAwait(false);
        return await ApplyAsync(InventoryMovementPlanner.PlanAdjustment(balance, delta, reason, DateTimeOffset.UtcNow, operationId), operationId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InventoryMovementPlan> ApplyAsync(InventoryMovementPlan plan, Guid operationId, CancellationToken cancellationToken)
    {
        var operation = new PendingSyncOperation(operationId, plan.Movement.WorkspaceId.Value, Guid.Empty, "Inventory", plan.Movement.ItemId, plan.Movement.Type.ToString(), 1, JsonSerializer.Serialize(plan.Movement), null, plan.Movement.OccurredUtc);
        await inventory.ApplyWithOutboxAsync(plan, operation, cancellationToken).ConfigureAwait(false);
        return plan;
    }

    private async Task<InventoryBalance> RequireBalanceAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, CancellationToken cancellationToken)
        => await inventory.GetBalanceAsync(workspaceId, itemId, placementId, cancellationToken).ConfigureAwait(false)
           ?? throw new InvalidOperationException("No balance exists for the requested placement.");

    private async Task<InventoryBalance> GetOrCreateAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, CancellationToken cancellationToken)
        => await inventory.GetBalanceAsync(workspaceId, itemId, placementId, cancellationToken).ConfigureAwait(false)
           ?? new InventoryBalance(workspaceId, itemId, placementId, 0);
}
