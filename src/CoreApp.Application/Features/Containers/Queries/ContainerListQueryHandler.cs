using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Features.Containers.Queries;

public sealed class ContainerListQueryHandler : IContainerListQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ContainerListQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    public Task<List<Container>> QueryAsync(bool emptyOnly, string? searchTerm = null, int? pageNumber = null, int? pageSize = null, TagFilter? tagFilter = null)
        => inventoryQueries.QueryContainersAsync(
            new ContainerListSpecification(
                Filter: emptyOnly ? ContainerQueryFilter.Empty : ContainerQueryFilter.All,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize,
                TagCriteria: tagFilter));

    public Task<int> CountAsync() => inventoryQueries.CountContainersAsync();
}
