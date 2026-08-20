using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

internal sealed record ContainerItemPageLoad(
	IReadOnlyList<ContainerItemInventoryEntry> Items,
	bool IsStale);
