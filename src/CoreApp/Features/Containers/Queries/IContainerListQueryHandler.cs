using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.Queries;

/// <summary>
/// Defines queries for listing containers.
/// </summary>
public interface IContainerListQueryHandler
{
    /// <param name="emptyOnly">The value used by the operation.</param>
    /// <param name="searchTerm">The value used by the operation.</param>
    /// <param name="pageNumber">The value used by the operation.</param>
    /// <param name="pageSize">The value used by the operation.</param>
    Task<List<Container>> QueryAsync(bool emptyOnly, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
}
