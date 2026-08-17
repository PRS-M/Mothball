using CoreApp.Entities.Inventory;
using CoreApp.Contracts;
using CoreApp.Specifications;

namespace CoreApp.Features.Items.Queries;

public sealed class ItemsListQueryHandler
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
        int? pageSize = null)
        => inventoryQueries.QueryInventorySnapshotsAsync(
            new ItemListSpecification(
                Filter: filter,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize));
}
