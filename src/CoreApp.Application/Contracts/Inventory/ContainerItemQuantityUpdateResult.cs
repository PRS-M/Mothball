namespace CoreApp.Application.Contracts.Inventory;

public sealed record ContainerItemQuantityUpdateResult(
    bool Removed,
    int TotalItemCount,
    int TotalQuantity,
    int AssignedQuantity,
    int UnassignedQuantity);