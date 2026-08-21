namespace CoreApp.Application.Contracts.Inventory;

public sealed record ContainerItemQuantityUpdateResult(int TotalItemCount, ItemInventoryUpdateResult Inventory);