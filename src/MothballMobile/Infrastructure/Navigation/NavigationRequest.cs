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

public sealed record AddItemNavigationRequest(
    Guid? ContainerId = null,
    Guid? TagId = null,
    string? TagName = null) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
    {
        var parameters = new Dictionary<string, object>();
        if (ContainerId is { } containerId)
        {
            parameters[NavigationParams.ContainerId] = containerId.ToString();
        }

        if (TagId is { } tagId)
        {
            parameters[NavigationParams.TagId] = tagId.ToString();
        }

        if (!string.IsNullOrWhiteSpace(TagName))
        {
            parameters[NavigationParams.TagName] = TagName;
        }

        return parameters;
    }
}

public sealed record AddContainerNavigationRequest(Guid? TagId = null, string? TagName = null) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
    {
        var parameters = new Dictionary<string, object>();
        if (TagId is { } tagId)
        {
            parameters[NavigationParams.TagId] = tagId.ToString();
        }

        if (!string.IsNullOrWhiteSpace(TagName))
        {
            parameters[NavigationParams.TagName] = TagName;
        }

        return parameters;
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

public sealed record TagResultsNavigationRequest(Guid TagId, string TagName) : INavigationRequest
{
    public IDictionary<string, object> ToParameters()
        => new Dictionary<string, object>
        {
            [NavigationParams.TagId] = TagId.ToString(),
            [NavigationParams.TagName] = TagName,
        };
}
