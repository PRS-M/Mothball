using CoreApp.Domain.Entities.InventoryAggregate;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

internal sealed record ContainerItemPageLoad(
	IReadOnlyList<ContainerItemInventoryEntry> Items,
	bool IsStale);
