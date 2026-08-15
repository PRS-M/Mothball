using CoreApp.Entities.Inventory;
using CoreApp.Contracts;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

internal sealed record ContainerItemPageLoad(
	IReadOnlyList<ContainerItemInventoryEntry> Items,
	bool IsStale);
