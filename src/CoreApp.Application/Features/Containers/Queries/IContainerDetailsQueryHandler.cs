using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Features.Containers.Queries;

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

    /// <summary>Queries the paged items stored in a container.</summary>
    /// <param name="containerId">The container identifier.</param>
    /// <param name="searchTerm">Optional free-text search.</param>
    /// <param name="pageNumber">The zero-based page number.</param>
    /// <param name="pageSize">The number of rows per page.</param>
    /// <param name="tagFilter">Optional exact tag criteria applied to items.</param>
    Task<List<ContainerItemInventoryEntry>> QueryItemsAsync(
        string containerId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        TagFilter? tagFilter = null);
}
