using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using Infrastructure.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Thin facade that composes focused repositories for a cohesive domain-oriented API.
/// </summary>
public class InventoryDomainRepository : IInventoryDomainRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;
    private readonly IImageRepository imageRepo;
    private readonly IRelationRepository relationRepo;

    public InventoryDomainRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IImageRepository imageRepo,
        IRelationRepository relationRepo)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.imageRepo = imageRepo;
        this.relationRepo = relationRepo;
    }

    #region Container Operations

    /// <inheritdoc />
    public Task<Container?> GetContainerAsync(string containerId)
        => containerRepo.GetAsync(containerId);

    /// <inheritdoc />
    public Task<List<Container>> GetAllContainersAsync()
        => containerRepo.GetAllAsync();

    /// <inheritdoc />
    public Task<List<Container>> GetAllContainersAsync(int pageNumber, int pageSize)
        => containerRepo.GetAllAsync(pageNumber, pageSize);

    /// <inheritdoc />
    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId)
    {
        var container = await containerRepo.GetWithItemsAndPhotosAsync(containerId);
        if (container is null) return null;

        // Avoid re-querying relations; use IDs already hydrated into the container aggregate
        var itemIds = container.Items.Select(s => s.ItemId);
        var items = await itemRepo.GetByIdsWithPhotosAsync(itemIds);
        return (container, items);
    }

    /// <inheritdoc />
    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize)
    {
        var container = await containerRepo.GetWithItemsAndPhotosAsync(containerId, pageNumber, pageSize);
        if (container is null) return null;

        // Avoid re-querying relations; use IDs already hydrated into the container aggregate
        var itemIds = container.Items.Select(s => s.ItemId);
        var items = await itemRepo.GetByIdsWithPhotosAsync(itemIds);
        return (container, items);
    }

    /// <inheritdoc />
    public Task<int> GetItemCountInContainerAsync(string containerId)
        => containerRepo.GetItemCountInContainerAsync(containerId);

    /// <inheritdoc />
    public Task<Container?> GetContainerForItemAsync(string itemId)
        => containerRepo.GetContainerForItemAsync(itemId);

    /// <inheritdoc />
    public Task InsertContainerAsync(Container container)
        => containerRepo.InsertAsync(container);

    /// <inheritdoc />
    public Task UpdateContainerAsync(Container container)
        => containerRepo.UpdateAsync(container);

    /// <inheritdoc />
    public Task DeleteContainerAsync(string containerId)
        => containerRepo.DeleteAsync(containerId);

    #endregion

    #region Item Operations

    /// <inheritdoc />
    public Task<List<Item>> GetItemsForContainerAsync(string containerId)
        => itemRepo.GetItemsForContainerAsync(containerId);

    /// <inheritdoc />
    public Task<Item?> GetItemWithPhotosAsync(string itemId)
        => itemRepo.GetWithPhotosAsync(itemId);

    /// <inheritdoc />
    public Task<List<Item>> GetAllItemsWithPhotosAsync()
        => itemRepo.GetAllWithPhotosAsync();

    /// <inheritdoc />
    public Task<List<Item>> GetAllItemsWithPhotosAsync(int pageNumber, int pageSize)
        => itemRepo.GetAllWithPhotosAsync(pageNumber, pageSize);

    /// <inheritdoc />
    public Task<List<Item>> GetUnassignedItemsWithPhotosAsync(int pageNumber, int pageSize)
        => itemRepo.GetUnassignedWithPhotosAsync(pageNumber, pageSize);

    /// <inheritdoc />
    public Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm)
        => itemRepo.SearchWithPhotosAsync(searchTerm);

    /// <inheritdoc />
    public Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
        => itemRepo.SearchItemsInContainerAsync(containerId, searchTerm, pageNumber, pageSize);

    /// <inheritdoc />
    public Task InsertItemAsync(Item item)
        => itemRepo.InsertAsync(item);

    /// <inheritdoc />
    public Task UpdateItemAsync(Item item)
        => itemRepo.UpdateAsync(item);

    /// <inheritdoc />
    public Task DeleteItemAsync(string itemId)
        => itemRepo.DeleteAsync(itemId);

    #endregion

    #region Image Operations

    /// <inheritdoc />
    public Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
        => imageRepo.InsertAsync(imageItem, ownerId);

    /// <inheritdoc />
    public Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
        => imageRepo.UpdateAsync(image, ownerId);

    #endregion

    #region Relation Operations

    /// <inheritdoc />
    public Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
        => relationRepo.InsertItemContainerRelationAsync(itemId, containerId, quantity);

    #endregion
}
