using CoreApp.Entities.Inventory;

namespace Infrastructure.Interfaces;

public interface IItemInventoryRepository
{
    Task<ItemInventory?> GetAsync(Guid itemId);
    Task InsertAsync(ItemInventory inventory);
    Task SaveAsync(ItemInventory inventory);
    Task DeleteAsync(Guid itemId);
}
