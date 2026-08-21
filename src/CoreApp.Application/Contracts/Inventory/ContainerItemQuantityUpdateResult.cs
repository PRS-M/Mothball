namespace CoreApp.Application.Contracts.Inventory;

public sealed record ContainerItemQuantityUpdateResult(
    bool Removed,
    int TotalItemCount,
    ItemInventoryUpdateResult Inventory);