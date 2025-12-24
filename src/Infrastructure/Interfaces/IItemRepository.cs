using CoreApp.Entities.ItemAggregate;

namespace Infrastructure.Interfaces;

/// <summary>
/// Repository for the Item aggregate root, including hydration of photos and container relations.
/// </summary>
public interface IItemRepository
{
    Task<Item?> GetWithPhotosAsync(string itemId);
    Task<List<Item>> GetAllWithPhotosAsync();
    Task<List<Item>> GetAllWithPhotosAsync(int pageNumber, int pageSize);
    Task<List<Item>> GetItemsForContainerAsync(string containerId);
    Task<List<Item>> GetByIdsWithPhotosAsync(IEnumerable<Guid> itemIds);
    Task<List<Item>> GetUnassignedWithPhotosAsync(int pageNumber, int pageSize);
    Task<List<Item>> SearchWithPhotosAsync(string searchTerm);
    Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize);
    Task InsertAsync(Item item);
    Task UpdateAsync(Item item);
    Task DeleteAsync(string itemId);
}
