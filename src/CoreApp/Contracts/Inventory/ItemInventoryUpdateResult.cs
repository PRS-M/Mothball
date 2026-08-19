namespace CoreApp.Contracts.Inventory;

public sealed record ItemInventoryUpdateResult(
    bool RemovedFromContainer,
    int TotalQuantity,
    int AssignedQuantity,
    int UnassignedQuantity,
    bool ItemDeleted = false);