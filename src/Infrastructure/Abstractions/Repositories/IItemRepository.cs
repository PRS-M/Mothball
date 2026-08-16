using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;

namespace Infrastructure.Abstractions.Repositories;

/// <summary>
/// Repository for the Item aggregate root, including hydration of photos and container relations.
/// </summary>
public interface IItemRepository
{
    /// <summary>
    /// Gets an item with its associated photos.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task<Item?> GetWithPhotosAsync(string itemId);
    /// <param name="specification">The value used by the operation.</param>
    Task<List<Item>> QueryWithPhotosAsync(ItemListSpecification specification);
    /// <param name="specification">The value used by the operation.</param>
    Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification);
    /// <summary>
    /// Inserts a new item.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    Task InsertAsync(Item item);
    /// <summary>
    /// Saves changes to an item.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    Task UpdateAsync(Item item);
    /// <summary>
    /// Deletes a photo from an item.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    /// <param name="imageId">The identifier used by the operation.</param>
    Task DeletePhotoAsync(Item item, Guid imageId);
    /// <summary>
    /// Deletes an item by its string identifier.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task DeleteAsync(string itemId);
}
