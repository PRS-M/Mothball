using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Features.Items.Queries;

public sealed class ItemsListQueryHandler : IItemsListQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ItemsListQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    public Task<List<InventorySnapshot>> QueryAsync(
        ItemQueryFilter filter,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null,
        TagFilter? tagFilter = null)
        => inventoryQueries.QueryInventorySnapshotsAsync(
            new ItemListSpecification(
                Filter: filter,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize,
                TagCriteria: tagFilter));
}
