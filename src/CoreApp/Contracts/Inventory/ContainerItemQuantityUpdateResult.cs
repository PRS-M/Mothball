namespace CoreApp.Contracts.Inventory;

public sealed record ContainerItemQuantityUpdateResult(bool Removed, int TotalItemCount);