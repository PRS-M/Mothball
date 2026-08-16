using CoreApp.Entities.Inventory;
using CoreApp.Contracts;
using CoreApp.Specifications;

namespace CoreApp.Interfaces;

public interface IItemsListQueryHandler
{
    Task<List<InventorySnapshot>> QueryAsync(
        ItemQueryFilter filter,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null);
}
