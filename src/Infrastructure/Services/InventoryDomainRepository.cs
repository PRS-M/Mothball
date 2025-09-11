using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class InventoryDomainRepository : IInventoryDomainRepository
{
    private readonly IRepository<DbContainer> containers;
    private readonly IRepository<DbItem> items;
    private readonly IRepository<DbImage> photos;
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;
    private readonly ILogger<InventoryDomainRepository> logger;

    public InventoryDomainRepository(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        ILogger<InventoryDomainRepository> logger)
    {
        this.containers = containers;
        this.items = items;
        this.photos = photos;
        this.itemContainerRelations = itemContainerRelations;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<Container?> GetContainerAsync(string containerId)
    {
    logger.LogDebug("GetContainerAsync: containerId={ContainerId}", containerId);
    var db = await containers.GetAsync(containerId);
        if (db is null) return null;

        // Load container photo if any
    var dbPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == db.ContainerId);
        return db.ToDomain(dbPhotos);
    }

    /// <inheritdoc />
    public async Task<List<Container>> GetAllContainersAsync()
    {
    var dbContainers = await containers.GetAllAsync();
        var containerIds = dbContainers.Select(c => (object)c.ContainerId).ToList();
    var photosList = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), containerIds);

    var photosByContainer = GroupPhotosByOwnerUniqueId(photosList);

        return MapDbContainersToDomain(dbContainers, photosByContainer);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetItemsForContainerAsync(string containerId)
    {
        // Compare using Guid to leverage indexes and avoid string conversions
        if (!Guid.TryParse(containerId, out var cid))
        {
            logger.LogWarning("GetItemsForContainerAsync: invalid containerId format: {ContainerId}", containerId);
            return new List<Item>();
        }

        IEnumerable<DbItemContainerRelation> dbItemContainerRelations =
            await itemContainerRelations.WhereAsync(r => r.ContainerId == cid);

        var itemIds = dbItemContainerRelations.Select(r => (object)r.ItemId).ToList();

    List<DbItem> itemsList = await items.WhereInAsync(nameof(DbItem.ItemId), itemIds);
    List<DbImage> photosForItems = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), itemIds);
    Dictionary<Guid, IEnumerable<DbImage>> photosByItem = GroupPhotosByOwnerUniqueId(photosForItems);

    return MapDbItemsToDomain(itemsList, photosByItem);
    }

    /// <inheritdoc />
    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId)
    {
        var container = await GetContainerAsync(containerId);
        if (container is null) return null;
    var itemsForContainer = await GetItemsForContainerAsync(containerId);
    return (container, itemsForContainer);
    }

    /// <inheritdoc />
    public async Task<Item?> GetItemWithPhotosAsync(string itemId)
    {
        logger.LogDebug("GetItemWithPhotosAsync: itemId={ItemId}", itemId);
    var dbItem = await items.GetAsync(itemId);
        if (dbItem is null) return null;

    var dbPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == dbItem.ItemId);
        return dbItem.ToDomain(dbPhotos);
    }

    /// <inheritdoc />
    public async Task<Container?> GetContainerForItemAsync(string itemId)
    {
        logger.LogDebug("GetContainerForItemAsync: itemId={ItemId}", itemId);
        if (!Guid.TryParse(itemId, out var iid)) return null;

        // Find relation
        var relation = (await itemContainerRelations.WhereAsync(r => r.ItemId == iid)).FirstOrDefault();
        if (relation is null) return null;

        // Load the container and its photo(s)
        var dbContainer = await containers.GetAsync(relation.ContainerId.ToString());
        if (dbContainer is null) return null;
        var dbPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == dbContainer.ContainerId);
        return dbContainer.ToDomain(dbPhotos);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetAllItemsWithPhotosAsync()
    {
        var itemsAll = await items.GetAllAsync();
        var itemIds = itemsAll.Select(i => (object)i.ItemId).ToList();
        var photosAll = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), itemIds);

        var photosByItem = GroupPhotosByOwnerUniqueId(photosAll);

        return MapDbItemsToDomain(itemsAll, photosByItem);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm)
    {
        // Case-insensitive search with index support using SQLite LIKE and NOCASE collation
        var pattern = $"%{searchTerm}%";
        var table = nameof(DbItem); // default table name used by sqlite-net
        var itemsQuery = await items.QueryAsync($"SELECT * FROM {table} WHERE Name LIKE ? COLLATE NOCASE", pattern);
        logger.LogDebug("GetItemsWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);
        var itemIds = itemsQuery.Select(i => (object)i.ItemId).ToList();
        var photosForQuery = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), itemIds);

        var photosByItem = GroupPhotosByOwnerUniqueId(photosForQuery);

        return MapDbItemsToDomain(itemsQuery, photosByItem);
    }

    /// <inheritdoc />
    public async Task InsertContainerAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        var dbContainer = container.ToDb();
        await containers.InsertAsync(dbContainer);
    }

    /// <inheritdoc />
    public async Task InsertItemAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var dbItem = item.ToDb();
        await items.InsertAsync(dbItem);
    }

    /// <inheritdoc />
    public async Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(imageItem);
        var dbImage = imageItem.ToDb(ownerId);
        await photos.InsertAsync(dbImage);
    }

    /// <inheritdoc />
    public async Task InsertItemContainerRelation(Guid itemId, Guid containerId)
    {
        var relation = new DbItemContainerRelation
        {
            ItemId = itemId,
            ContainerId = containerId
        };

        await itemContainerRelations.InsertAsync(relation);
    }

    /// <inheritdoc />
    public async Task UpdateContainerAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        var dbContainer = container.ToDb();
        await containers.UpdateAsync(dbContainer);
    }

    /// <inheritdoc />
    public async Task UpdateItemAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var dbItem = item.ToDb();
        await items.UpdateAsync(dbItem);
    }

    /// <inheritdoc />
    public async Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(image);
        var dbImage = image.ToDb(ownerId);
        await photos.UpdateAsync(dbImage);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var iid)) return;

        // Delete item images
        var images = await photos.WhereAsync(p => p.OwnerUniqueId == iid);
        foreach (var img in images)
        {
            await photos.DeleteAsync(img);
        }

        // Delete relations
        var relations = await itemContainerRelations.WhereAsync(r => r.ItemId == iid);
        foreach (var rel in relations)
        {
            await itemContainerRelations.DeleteAsync(rel);
        }

        // Delete item
        var dbItem = await items.GetAsync(itemId);
        if (dbItem is not null)
        {
            await items.DeleteAsync(dbItem);
        }
    }

    /// <inheritdoc />
    public async Task DeleteContainerAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return;

        // Delete container images
        var images = await photos.WhereAsync(p => p.OwnerUniqueId == cid);
        foreach (var img in images)
        {
            await photos.DeleteAsync(img);
        }

        // Delete relations (items remain)
        var relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == cid);
        foreach (var rel in relations)
        {
            await itemContainerRelations.DeleteAsync(rel);
        }

        // Delete container
        var dbContainer = await containers.GetAsync(containerId);
        if (dbContainer is not null)
        {
            await containers.DeleteAsync(dbContainer);
        }
    }

    private static List<Item> MapDbItemsToDomain(List<DbItem> items, Dictionary<Guid, IEnumerable<DbImage>> photosByItem)
    {
        var domain = new List<Item>(items.Count);
        foreach (var dbItem in items)
        {
            photosByItem.TryGetValue(dbItem.ItemId, out var dbPhotosForItem);
            domain.Add(dbItem.ToDomain(dbPhotosForItem));
        }

        return domain;
    }

    private static List<Container> MapDbContainersToDomain(List<DbContainer> dbContainers, Dictionary<Guid, IEnumerable<DbImage>> photosByContainer)
    {
        var domainContainers = new List<Container>(dbContainers.Count);
        foreach (var dbContainer in dbContainers)
        {
            photosByContainer.TryGetValue(dbContainer.ContainerId, out var dbPhotosForContainer);
            domainContainers.Add(dbContainer.ToDomain(dbPhotosForContainer));
        }

        return domainContainers;
    }

    private static Dictionary<Guid, IEnumerable<DbImage>> GroupPhotosByOwnerUniqueId(List<DbImage> photos)
    {
        return photos
            .GroupBy(p => p.OwnerUniqueId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }
}
