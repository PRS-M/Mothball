using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Contracts;

public sealed record ItemDetailsResult(
	Item Item,
	Guid? ContainerId,
	IReadOnlyList<ItemContainerAllocation>? Allocations = null);
