using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;

namespace CoreApp.Interfaces;

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
    /// Loads all Containers with their photos (if any).
    /// </summary>
    /// <returns>List of Containers.</returns>
    Task<List<Container>> GetAllContainersAsync();

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
    /// Loads items with their photos filtered by a domain-level predicate.
    /// The predicate is defined on the domain model and must only reference
    /// properties that are persisted (e.g., UniqueId, Name).
    /// </summary>
    /// <param name="predicate">Domain predicate for filtering items.</param>
    /// <returns>A list of Items with their photos loaded.</returns>
    Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm);

    /// <summary>
    /// Loads a domain Item together with its photos.
    /// </summary>
    /// <param name="itemId">The item UniqueId.</param>
    /// <returns>The Item or null if not found.</returns>
    Task<Item?> GetItemWithPhotosAsync(string itemId);

    Task InsertContainerAsync(Container container);

    Task InsertItemAsync(Item item);

    Task InsertImageItem(ImageItem imageItem);

    Task UpdateContainerAsync(Container container);

    Task UpdateItemAsync(Item item);

    Task UpdateImageItemAsync(ImageItem image, string ownerId);
}
