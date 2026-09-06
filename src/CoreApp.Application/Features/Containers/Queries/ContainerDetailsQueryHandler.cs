using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Features.Containers.Queries;

public sealed class ContainerDetailsQueryHandler : IContainerDetailsQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ContainerDetailsQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    /// <inheritdoc />
    public async Task<ContainerDetailsResult?> GetDetailsAsync(string containerId)
    {
        var container = await inventoryQueries.GetContainerAsync(containerId);
        if (container is null)
        {
            return null;
        }

        var totalItemCount = await inventoryQueries.GetItemCountInContainerAsync(containerId);
        return new ContainerDetailsResult(container, totalItemCount);
    }

    /// <inheritdoc />
    public Task<int> GetDistinctItemCountAsync(string containerId)
        => inventoryQueries.GetDistinctItemCountInContainerAsync(containerId);

    /// <inheritdoc />
    public Task<List<ContainerItemInventoryEntry>> QueryItemsAsync(
        string containerId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        TagFilter? tagFilter = null)
        => inventoryQueries.QueryContainerItemInventoryAsync(
            new ContainerItemsSpecification(
                ContainerId: containerId,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize,
                TagCriteria: tagFilter));
}
