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

public sealed record ItemDetailsNavigationRequest(Guid ItemId, Guid? SourceContainerId = null) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
    {
        var parameters = new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = ItemId.ToString(),
        };

        if (SourceContainerId is { } sourceContainerId)
        {
            parameters[NavigationParams.ContainerId] = sourceContainerId.ToString();
        }

        return parameters;
    }
}

public sealed record AddItemNavigationRequest(Guid? ContainerId = null) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
    {
        if (ContainerId is not { } containerId)
        {
            return new Dictionary<string, object>();
        }

        return new Dictionary<string, object>
        {
            [NavigationParams.ContainerId] = containerId.ToString(),
        };
    }
}

public sealed record AddExistingItemToContainerNavigationRequest(Guid ContainerId) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
        => new Dictionary<string, object>
        {
            [NavigationParams.ContainerId] = ContainerId.ToString(),
        };
}