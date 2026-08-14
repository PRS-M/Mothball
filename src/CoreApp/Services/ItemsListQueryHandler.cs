using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;

namespace CoreApp.Services;

public sealed class ItemsListQueryHandler : IItemsListQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ItemsListQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    public Task<List<Item>> QueryAsync(bool unassignedOnly, string? searchTerm = null, int? pageNumber = null, int? pageSize = null)
        => inventoryQueries.QueryItemsWithPhotosAsync(
            new ItemListSpecification(
                Filter: unassignedOnly ? ItemQueryFilter.Unassigned : ItemQueryFilter.All,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize));
}
