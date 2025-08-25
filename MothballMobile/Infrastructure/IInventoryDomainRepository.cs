using System.Linq.Expressions;
using CoreApp.Models;
using MothballMobile.Infrastructure.DatabaseModels;

namespace MothballMobile.Infrastructure;

/// <summary>
/// Domain-oriented repository that composes SQLite entities into rich domain models.
/// </summary>
public interface IInventoryDomainRepository
{
    /// <summary>
    /// Loads a domain Container, including its optional photo.
    /// </summary>
    /// <param name="containerId">The container UniqueId.</param>
    /// <returns>The Container or null if not found.</returns>
    Task<Container?> GetContainerAsync(string containerId);

    /// <summary>
    /// Loads domain Items for a container, each with its photos (filenames and any stored image data).
    /// </summary>
    /// <param name="containerId">The container UniqueId.</param>
    /// <returns>List of Items.</returns>
    Task<List<Item>> GetItemsForContainerAsync(string containerId);

    /// <summary>
    /// Loads a Container and its Items (with photos) in a single call.
    /// </summary>
    /// <param name="containerId">The container UniqueId.</param>
    /// <returns>Tuple of container and items, or null if container not found.</returns>
    Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId);

    /// <summary>
    /// Loads all Items with their photos.
    /// </summary>
    /// <returns>A list of Items with their photos loaded.</returns>
    Task<List<Item>> GetAllItemsWithPhotosAsync();

    /// <summary>
    /// Loads items with their photos
    /// </summary>
    /// <param name="predicate">Predicate for filtering of the items.</param>
    /// <returns>A list of Items with their photos loaded.</returns>
    Task<List<Item>> GetItemsWithPhotosAsync(Expression<Func<DbItem, bool>> predicate);

    /// <summary>
    /// Loads a domain Item together with its photos.
    /// </summary>
    /// <param name="itemId">The item UniqueId.</param>
    /// <returns>The Item or null if not found.</returns>
    Task<Item?> GetItemWithPhotosAsync(string itemId);
}
