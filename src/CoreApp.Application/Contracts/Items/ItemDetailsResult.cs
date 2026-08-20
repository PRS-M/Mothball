using CoreApp.Domain.Entities.InventoryAggregate;

namespace CoreApp.Application.Contracts.Items;

public sealed record ItemDetailsResult(InventorySnapshot Inventory);