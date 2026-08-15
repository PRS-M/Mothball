using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;
using CoreApp.Contracts;

namespace CoreApp.Services;

public sealed class ContainerAssociationQueryHandler : IContainerAssociationQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ContainerAssociationQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    public Task<List<Container>> QueryContainersAsync(int pageNumber, int pageSize)
        => inventoryQueries.QueryContainersAsync(
            new ContainerListSpecification(
                Filter: ContainerQueryFilter.All,
                PageNumber: pageNumber,
                PageSize: pageSize));

    public Task<List<ItemInventorySummary>> QueryUnassignedItemsAsync(int pageNumber, int pageSize)
        => inventoryQueries.QueryItemInventorySummariesAsync(
            new ItemListSpecification(
                Filter: ItemQueryFilter.Unassigned,
                PageNumber: pageNumber,
                PageSize: pageSize));
}
