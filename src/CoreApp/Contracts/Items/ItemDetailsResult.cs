using CoreApp.Entities.Inventory;

namespace CoreApp.Contracts.Items;

public sealed record ItemDetailsResult(InventorySnapshot Inventory);