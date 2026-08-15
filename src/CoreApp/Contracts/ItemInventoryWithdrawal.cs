namespace CoreApp.Contracts;

public sealed record ItemContainerAllocation(Guid ContainerId, string ContainerName, int Quantity);

public sealed record ItemAllocationWithdrawal(Guid ContainerId, int Quantity);

public sealed record ItemInventoryWithdrawalPlan(
    int TotalQuantity,
    int AssignedQuantity,
    int UnassignedQuantity,
    IReadOnlyList<ItemContainerAllocation> Allocations,
    bool DeleteItem);