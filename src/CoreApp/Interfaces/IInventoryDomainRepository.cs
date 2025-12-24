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
    /// Loads Containers with their photos (if any) using paging.
    /// </summary>
    /// <param name="pageNumber">The page number to load.</param>
    /// <param name="pageSize">The number of containers per page.</param>
    /// <returns>List of Containers for the requested page.</returns>
    Task<List<Container>> GetAllContainersAsync(int pageNumber, int pageSize);

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
    /// Loads a Container and a paginated list of its Items (with photos).
    /// </summary>
    /// <param name="containerId">The container UniqueId.</param>
    /// <param name="pageNumber">The page number to load.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>Tuple of container and paginated items, or null if container not found.</returns>
    Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize);

    /// <summary>
    /// Gets the total count of items in a specific container.
    /// </summary>
    /// <param name="containerId">The container UniqueId.</param>
    /// <returns>The total number of items in the container.</returns>
    Task<int> GetItemCountInContainerAsync(string containerId);

    /// <summary>
    /// Loads all Items with their photos.
    /// </summary>
    /// <returns>A list of Items with their photos loaded.</returns>
    Task<List<Item>> GetAllItemsWithPhotosAsync();

    Task<List<Item>> GetAllItemsWithPhotosAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Loads items that are not related to any container, with their photos, using paging.
    /// </summary>
    Task<List<Item>> GetUnassignedItemsWithPhotosAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Loads items with their photos filtered by a search term applied to persisted properties
    /// (for example, name or other searchable fields).
    /// </summary>
    /// <param name="searchTerm">The search term used to filter items.</param>
    /// <returns>A list of Items with their photos loaded.</returns>
    Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm);

    /// <summary>
    /// Searches items within a specific container with pagination.
    /// </summary>
    /// <param name="containerId">The container UniqueId.</param>
    /// <param name="searchTerm">The search term to filter items.</param>
    /// <param name="pageNumber">The page number to load.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of Items with their photos loaded.</returns>
    Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize);

    /// <summary>
    /// Loads a domain Item together with its photos.
    /// </summary>
    /// <param name="itemId">The item UniqueId.</param>
    /// <returns>The Item or null if not found.</returns>
    Task<Item?> GetItemWithPhotosAsync(string itemId);

    /// <summary>
    /// Finds the container that contains the specified item, if any.
    /// </summary>
    /// <param name="itemId">The item UniqueId.</param>
    /// <returns>The Container or null if the item is not related to any container.</returns>
    Task<Container?> GetContainerForItemAsync(string itemId);

    /// <summary>
    /// Inserts a new container into the data store.
    /// </summary>
    /// <param name="container">The domain container to insert.</param>
    Task InsertContainerAsync(Container container);

    /// <summary>
    /// Inserts a new item into the data store.
    /// </summary>
    /// <param name="item">The domain item to insert.</param>
    Task InsertItemAsync(Item item);

    /// <summary>
    /// Inserts a new image and associates it with the owning aggregate (container or item).
    /// </summary>
    /// <param name="imageItem">The image to insert.</param>
    /// <param name="ownerId">The unique identifier of the owner aggregate.</param>
    Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId);

    /// <summary>
    /// Creates a relation between an Item and a Container.
    /// </summary>
    /// <param name="itemId">Item unique id (Guid).</param>
    /// <param name="containerId">Container unique id (Guid).</param>
    /// <param name="quantity">Quantity of the item stored in the container.</param>
    Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity);

    /// <summary>
    /// Updates an existing container in the data store.
    /// </summary>
    /// <param name="container">The container with updated state.</param>
    Task UpdateContainerAsync(Container container);

    /// <summary>
    /// Updates an existing item in the data store.
    /// </summary>
    /// <param name="item">The item with updated state.</param>
    Task UpdateItemAsync(Item item);

    /// <summary>
    /// Updates an existing image associated with the specified owner aggregate.
    /// </summary>
    /// <param name="image">The image with updated state.</param>
    /// <param name="ownerId">The unique identifier of the owner aggregate.</param>
    Task UpdateImageItemAsync(ImageItem image, Guid ownerId);

    /// <summary>
    /// Deletes an item and any related images and container relations.
    /// </summary>
    Task DeleteItemAsync(string itemId);

    /// <summary>
    /// Deletes a container and any related images and item relations.
    /// Items themselves are not deleted.
    /// </summary>
    Task DeleteContainerAsync(string containerId);
}
