using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Features.Containers.Queries;

/// <summary>
/// Defines queries for listing containers.
/// </summary>
public interface IContainerListQueryHandler
{
    /// <summary>Gets the total number of containers, independent of list filters.</summary>
    Task<int> CountAsync();
    /// <param name="emptyOnly">The value used by the operation.</param>
    /// <param name="searchTerm">The value used by the operation.</param>
    /// <param name="pageNumber">The value used by the operation.</param>
    /// <param name="pageSize">The value used by the operation.</param>
    /// <param name="tagFilter">Optional exact tag criteria applied alongside the text query.</param>
    Task<List<Container>> QueryAsync(bool emptyOnly, string? searchTerm = null, int? pageNumber = null, int? pageSize = null, TagFilter? tagFilter = null);
}
