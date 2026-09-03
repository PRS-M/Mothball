using CoreApp.Domain.Entities.InventoryAggregate;

namespace CoreApp.Domain.Inventory;

/// <summary>Validates canonical movement rules and calculates resulting balances.</summary>
public static class InventoryMovementPlanner
{
    /// <summary>Plans a receipt without allowing a negative or cross-item balance.</summary>
    public static InventoryMovementPlan PlanReceipt(InventoryBalance destination, int quantity, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var movement = InventoryMovement.Receipt(destination.WorkspaceId, destination.ItemId, destination.PlacementId, quantity, reason, occurredUtc, movementId);
        return new(movement, [destination.Add(quantity)]);
    }

    /// <summary>Plans a withdrawal from an existing placement.</summary>
    public static InventoryMovementPlan PlanWithdrawal(InventoryBalance source, int quantity, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (quantity > source.OnHandQuantity) throw new InvalidOperationException("Withdrawal cannot exceed on-hand quantity.");
        var movement = InventoryMovement.Withdrawal(source.WorkspaceId, source.ItemId, source.PlacementId, quantity, reason, occurredUtc, movementId);
        return new(movement, [source.Add(-quantity)]);
    }

    /// <summary>Plans a transfer while preserving total on-hand quantity.</summary>
    public static InventoryMovementPlan PlanTransfer(InventoryBalance source, InventoryBalance destination, int quantity, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (source.WorkspaceId != destination.WorkspaceId || source.ItemId != destination.ItemId)
            throw new InvalidOperationException("A transfer must remain within one workspace and item.");
        if (quantity > source.OnHandQuantity) throw new InvalidOperationException("Transfer cannot exceed source on-hand quantity.");
        var movement = InventoryMovement.Transfer(source.WorkspaceId, source.ItemId, source.PlacementId, destination.PlacementId, quantity, reason, occurredUtc, movementId);
        return new(movement, [source.Add(-quantity), destination.Add(quantity)]);
    }

    /// <summary>Plans a positive or negative adjustment at one placement.</summary>
    public static InventoryMovementPlan PlanAdjustment(InventoryBalance balance, int delta, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
    {
        ArgumentNullException.ThrowIfNull(balance);
        if (delta < 0 && -delta > balance.OnHandQuantity) throw new InvalidOperationException("Adjustment cannot reduce on-hand quantity below zero.");
        var movement = InventoryMovement.Adjustment(balance.WorkspaceId, balance.ItemId, balance.PlacementId, delta, reason, occurredUtc, movementId);
        return new(movement, [balance.Add(delta)]);
    }
}

/// <summary>Represents a validated movement and its resulting materialized balances.</summary>
public sealed record InventoryMovementPlan(InventoryMovement Movement, IReadOnlyList<InventoryBalance> ResultingBalances);
