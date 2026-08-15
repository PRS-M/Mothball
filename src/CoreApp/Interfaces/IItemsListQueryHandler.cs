using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IItemsListQueryHandler
{
    Task<List<ItemInventorySummary>> QueryAsync(
        bool unassignedOnly,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null);
}
