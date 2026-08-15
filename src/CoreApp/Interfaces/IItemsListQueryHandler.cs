using CoreApp.Entities.Inventory;
using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IItemsListQueryHandler
{
    Task<List<InventorySnapshot>> QueryAsync(
        bool unassignedOnly,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null);
}
