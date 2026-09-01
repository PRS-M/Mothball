using CoreApp.Domain.Entities.InventoryAggregate;

namespace Infrastructure.Abstractions.Repositories;

/// <summary>
/// Defines persistence operations for item inventory aggregates.
/// </summary>
public interface IItemInventoryRepository
{
    /// <summary>
    /// Gets the inventory record for an item.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task<ItemInventory?> GetAsync(Guid itemId);

    /// <summary>
    /// Gets inventory records for a set of items in one batch.
    /// </summary>
    /// <param name="itemIds">The item identifiers to load.</param>
    Task<IReadOnlyDictionary<Guid, ItemInventory>> GetManyAsync(IReadOnlyCollection<Guid> itemIds);

    /// <summary>
    /// Inserts a new item inventory record.
    /// </summary>
    /// <param name="inventory">The value used by the operation.</param>
    Task InsertAsync(ItemInventory inventory);
    /// <summary>
    /// Saves changes to an item inventory record.
    /// </summary>
    /// <param name="inventory">The value used by the operation.</param>
    Task SaveAsync(ItemInventory inventory);
    /// <summary>
    /// Deletes an item's inventory record.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task DeleteAsync(Guid itemId);
}
