namespace CoreApp.Domain.Entities.InventoryAggregate;

/// <summary>Identifies the workspace that owns synchronized inventory.</summary>
public readonly record struct InventoryWorkspaceId
{
    /// <summary>Creates a workspace identifier.</summary>
    public InventoryWorkspaceId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>Gets the stable identifier value.</summary>
    public Guid Value { get; }
}

/// <summary>Identifies the physical or virtual placement of stock.</summary>
public readonly record struct InventoryPlacementId
{
    /// <summary>Creates a placement identifier.</summary>
    public InventoryPlacementId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Placement ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>Gets the stable identifier value.</summary>
    public Guid Value { get; }
}

/// <summary>Describes the canonical stock operation recorded in the movement ledger.</summary>
public enum InventoryMovementType
{
    Receipt,
    Withdrawal,
    Transfer,
    Adjustment,
}

/// <summary>Describes an immutable canonical inventory movement.</summary>
public sealed record InventoryMovement
{
    private InventoryMovement(
        Guid movementId,
        InventoryWorkspaceId workspaceId,
        Guid itemId,
        InventoryMovementType type,
        int quantity,
        InventoryPlacementId? sourcePlacementId,
        InventoryPlacementId? destinationPlacementId,
        string reason,
        DateTimeOffset occurredUtc)
    {
        if (movementId == Guid.Empty) throw new ArgumentException("Movement ID cannot be empty.", nameof(movementId));
        if (itemId == Guid.Empty) throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required.", nameof(reason));

        MovementId = movementId;
        WorkspaceId = workspaceId;
        ItemId = itemId;
        Type = type;
        Quantity = quantity;
        SourcePlacementId = sourcePlacementId;
        DestinationPlacementId = destinationPlacementId;
        Reason = reason.Trim();
        OccurredUtc = occurredUtc;
    }

    /// <summary>Creates a receipt into a placement.</summary>
    public static InventoryMovement Receipt(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId destination, int quantity, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
        => Create(movementId, workspaceId, itemId, InventoryMovementType.Receipt, quantity, null, destination, reason, occurredUtc);

    /// <summary>Creates a withdrawal from a placement.</summary>
    public static InventoryMovement Withdrawal(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId source, int quantity, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
        => Create(movementId, workspaceId, itemId, InventoryMovementType.Withdrawal, quantity, source, null, reason, occurredUtc);

    /// <summary>Creates a physical transfer between two placements.</summary>
    public static InventoryMovement Transfer(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId source, InventoryPlacementId destination, int quantity, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
    {
        if (source == destination) throw new ArgumentException("Source and destination must differ.", nameof(destination));
        return Create(movementId, workspaceId, itemId, InventoryMovementType.Transfer, quantity, source, destination, reason, occurredUtc);
    }

    /// <summary>Creates a signed quantity adjustment at one placement.</summary>
    public static InventoryMovement Adjustment(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placement, int delta, string reason, DateTimeOffset occurredUtc, Guid? movementId = null)
    {
        if (delta == 0) throw new ArgumentOutOfRangeException(nameof(delta), "An adjustment cannot be zero.");
        return Create(movementId, workspaceId, itemId, InventoryMovementType.Adjustment, Math.Abs(delta), delta > 0 ? null : placement, delta > 0 ? placement : null, reason, occurredUtc);
    }

    /// <summary>Gets the stable movement identifier.</summary>
    public Guid MovementId { get; }
    /// <summary>Gets the owning workspace.</summary>
    public InventoryWorkspaceId WorkspaceId { get; }
    /// <summary>Gets the item whose stock changed.</summary>
    public Guid ItemId { get; }
    /// <summary>Gets the operation type.</summary>
    public InventoryMovementType Type { get; }
    /// <summary>Gets the positive quantity moved.</summary>
    public int Quantity { get; }
    /// <summary>Gets the source placement, when applicable.</summary>
    public InventoryPlacementId? SourcePlacementId { get; }
    /// <summary>Gets the destination placement, when applicable.</summary>
    public InventoryPlacementId? DestinationPlacementId { get; }
    /// <summary>Gets the required human-readable reason.</summary>
    public string Reason { get; }
    /// <summary>Gets when the operation occurred.</summary>
    public DateTimeOffset OccurredUtc { get; }

    private static InventoryMovement Create(Guid? movementId, InventoryWorkspaceId workspaceId, Guid itemId, InventoryMovementType type, int quantity, InventoryPlacementId? source, InventoryPlacementId? destination, string reason, DateTimeOffset occurredUtc)
        => new(movementId ?? Guid.NewGuid(), workspaceId, itemId, type, quantity, source, destination, reason, occurredUtc);
}

/// <summary>Materialized on-hand quantity for one item and placement.</summary>
public sealed record InventoryBalance
{
    /// <summary>Creates a validated inventory balance.</summary>
    public InventoryBalance(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, int onHandQuantity, long version = 0)
    {
        if (itemId == Guid.Empty) throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
        if (onHandQuantity < 0) throw new ArgumentOutOfRangeException(nameof(onHandQuantity));
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
        WorkspaceId = workspaceId;
        ItemId = itemId;
        PlacementId = placementId;
        OnHandQuantity = onHandQuantity;
        Version = version;
    }

    /// <summary>Gets the owning workspace.</summary>
    public InventoryWorkspaceId WorkspaceId { get; }
    /// <summary>Gets the item identifier.</summary>
    public Guid ItemId { get; }
    /// <summary>Gets the placement identifier.</summary>
    public InventoryPlacementId PlacementId { get; }
    /// <summary>Gets current on-hand quantity.</summary>
    public int OnHandQuantity { get; }
    /// <summary>Gets the optimistic-concurrency version.</summary>
    public long Version { get; }

    internal InventoryBalance Add(int delta)
        => new(WorkspaceId, ItemId, PlacementId, checked(OnHandQuantity + delta), Version + 1);
}
