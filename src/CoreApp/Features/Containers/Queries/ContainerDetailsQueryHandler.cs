using CoreApp.Entities.Inventory;
using CoreApp.Contracts;
using CoreApp.Specifications;

namespace CoreApp.Features.Containers.Queries;

public sealed class ContainerDetailsQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ContainerDetailsQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

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

    public Task<int> GetDistinctItemCountAsync(string containerId)
        => inventoryQueries.GetDistinctItemCountInContainerAsync(containerId);

    public Task<List<ContainerItemInventoryEntry>> QueryItemsAsync(
        string containerId,
        string? searchTerm,
        int pageNumber,
        int pageSize)
        => inventoryQueries.QueryContainerItemInventoryAsync(
            new ContainerItemsSpecification(
                ContainerId: containerId,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize));
}
