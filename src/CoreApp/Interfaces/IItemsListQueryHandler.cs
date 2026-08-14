using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Interfaces;

public interface IItemsListQueryHandler
{
    Task<List<Item>> QueryAsync(bool unassignedOnly, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
}
