namespace CoreApp.Domain.Entities.InventoryAggregate;

public sealed record ItemContainerAllocation(Guid ContainerId, string ContainerName, int Quantity);

public sealed record ItemAllocationWithdrawal(Guid ContainerId, int Quantity);

public enum ItemInventoryConsumptionSourceKind
{
    Container,
    Unassigned,
}

public sealed record ItemInventoryConsumptionSource(
    ItemInventoryConsumptionSourceKind Kind,
    Guid? ContainerId = null)
{
    public static ItemInventoryConsumptionSource FromContainer(Guid containerId)
        => new(ItemInventoryConsumptionSourceKind.Container, containerId);

    public static ItemInventoryConsumptionSource FromUnassigned()
        => new(ItemInventoryConsumptionSourceKind.Unassigned);
}

public sealed record ItemInventoryWithdrawalPlan(
    int TotalQuantity,
    int AssignedQuantity,
    int UnassignedQuantity,
    IReadOnlyList<ItemContainerAllocation> Allocations,
    bool DeleteItem);
