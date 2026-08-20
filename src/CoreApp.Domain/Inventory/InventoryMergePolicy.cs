namespace CoreApp.Domain.Inventory;

/// <summary>
/// Defines how inventory data from an external source is merged with existing inventory.
/// </summary>
public sealed record InventoryMergePolicy(
    bool AllowMetadataUpdates,
    bool DeleteMissingRoots,
    InventoryChildReconciliationMode ChildReconciliationMode)
{
    public static InventoryMergePolicy AddOnly { get; } = new(
        AllowMetadataUpdates: false,
        DeleteMissingRoots: false,
        ChildReconciliationMode: InventoryChildReconciliationMode.Additive);

    public static InventoryMergePolicy AddAndUpsertMetadata { get; } = new(
        AllowMetadataUpdates: true,
        DeleteMissingRoots: false,
        ChildReconciliationMode: InventoryChildReconciliationMode.Additive);

    public static InventoryMergePolicy FullSync { get; } = new(
        AllowMetadataUpdates: true,
        DeleteMissingRoots: true,
        ChildReconciliationMode: InventoryChildReconciliationMode.Additive);

    public static InventoryMergePolicy StrictFullSync { get; } = new(
        AllowMetadataUpdates: true,
        DeleteMissingRoots: true,
        ChildReconciliationMode: InventoryChildReconciliationMode.Exact);
}

/// <summary>
/// Defines how relations and images are reconciled during an inventory merge.
/// </summary>
public enum InventoryChildReconciliationMode
{
    Additive,
    Exact,
}