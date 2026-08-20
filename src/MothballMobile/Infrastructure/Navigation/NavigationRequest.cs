namespace MothballMobile.Infrastructure.Navigation;

public interface INavigationRequest
{
    IDictionary<string, object> ToParameters();
}

public sealed record ContainerDetailsNavigationRequest(Guid ContainerId) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
        => new Dictionary<string, object>
        {
            [NavigationParams.ContainerId] = ContainerId.ToString(),
        };
}

public sealed record ItemLocationsNavigationRequest(Guid ItemId) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
        => new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = ItemId.ToString(),
        };
}

public sealed record AssociateItemWithContainerNavigationRequest(Guid ItemId, int UnassignedQuantity) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
        => new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = ItemId.ToString(),
            [NavigationParams.UnassignedQuantity] = UnassignedQuantity,
        };
}