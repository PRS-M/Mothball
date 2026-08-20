using CoreApp.Entities.InventoryAggregate;
using CoreApp.Contracts;

namespace CoreApp.Features.Containers.Queries;

/// <summary>
/// Defines queries for container details and contents.
/// </summary>
public interface IContainerDetailsQueryHandler
{
    /// <summary>
    /// Gets the details for a container.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task<ContainerDetailsResult?> GetDetailsAsync(string containerId);

    /// <summary>
    /// Gets the number of distinct items stored in a container.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task<int> GetDistinctItemCountAsync(string containerId);

    Task<List<ContainerItemInventoryEntry>> QueryItemsAsync(
        string containerId,
        string? searchTerm,
        int pageNumber,
        int pageSize);
}
