using CoreApp.Entities.ItemAggregate;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

internal sealed record ContainerItemPageLoad(IReadOnlyList<Item> Items, bool IsStale);
