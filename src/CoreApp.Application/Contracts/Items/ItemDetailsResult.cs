using CoreApp.Entities.InventoryAggregate;

namespace CoreApp.Contracts.Items;

public sealed record ItemDetailsResult(InventorySnapshot Inventory);