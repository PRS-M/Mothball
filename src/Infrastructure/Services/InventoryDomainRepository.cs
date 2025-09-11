using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using Infrastructure.Interfaces;
using MothballMobile.Infrastructure.DatabaseModels;
using MothballMobile.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;

namespace MothballMobile.Infrastructure;

public class InventoryDomainRepository : IInventoryDomainRepository
{
    private readonly IRepository<DbContainer> _containers;
    private readonly IRepository<DbItem> _items;
    private readonly IRepository<DbImage> _photos;
    private readonly IRepository<DbItemContainerRelation> _itemContainerRelations;
    private readonly ILogger<InventoryDomainRepository> _logger;

    public InventoryDomainRepository(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        ILogger<InventoryDomainRepository> logger)
    {
        _containers = containers;
        _items = items;
        _photos = photos;
        _itemContainerRelations = itemContainerRelations;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Container?> GetContainerAsync(string containerId)
    {
        _logger.LogDebug("GetContainerAsync: containerId={ContainerId}", containerId);
        var db = await _containers.GetAsync(containerId);
        if (db is null) return null;

        // Load container photo if any
        var dbPhotos = await _photos.WhereAsync(p => p.OwnerUniqueId == db.ContainerId);
        return db.ToDomain(dbPhotos);
    }

    public async Task<List<Container>> GetAllContainersAsync()
    {
        var dbContainers = await _containers.GetAllAsync();
        var containerIds = dbContainers.Select(c => (object)c.ContainerId).ToList();
        var photos = await _photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), containerIds);

        var photosByContainer = GroupPhotosByOwnerUniqueId(photos);

        return MapDbContainersToDomain(dbContainers, photosByContainer);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetItemsForContainerAsync(string containerId)
    {
        // Compare using Guid to leverage indexes and avoid string conversions
        if (!Guid.TryParse(containerId, out var cid))
        {
            _logger.LogWarning("GetItemsForContainerAsync: invalid containerId format: {ContainerId}", containerId);
            return new List<Item>();
        }

        IEnumerable<DbItemContainerRelation> dbItemContainerRelations =
            await _itemContainerRelations.WhereAsync(r => r.ContainerId == cid);

        var itemIds = dbItemContainerRelations.Select(r => (object)r.ItemId).ToList();

        List<DbItem> items = await _items.WhereInAsync(nameof(DbItem.ItemId), itemIds);
        List<DbImage> photos = await _photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), itemIds);
        Dictionary<Guid, IEnumerable<DbImage>> photosByItem = GroupPhotosByOwnerUniqueId(photos);

        return MapDbItemsToDomain(items, photosByItem);
    }

    /// <inheritdoc />
    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId)
    {
        var container = await GetContainerAsync(containerId);
        if (container is null) return null;
        var items = await GetItemsForContainerAsync(containerId);
        return (container, items);
    }

    /// <inheritdoc />
    public async Task<Item?> GetItemWithPhotosAsync(string itemId)
    {
    _logger.LogDebug("GetItemWithPhotosAsync: itemId={ItemId}", itemId);
        var dbItem = await _items.GetAsync(itemId);
        if (dbItem is null) return null;

        var dbPhotos = await _photos.WhereAsync(p => p.OwnerUniqueId == dbItem.ItemId);
        return dbItem.ToDomain(dbPhotos);
    }

    public async Task<Container?> GetContainerForItemAsync(string itemId)
    {
        _logger.LogDebug("GetContainerForItemAsync: itemId={ItemId}", itemId);
        if (!Guid.TryParse(itemId, out var iid)) return null;

        // Find relation
        var relation = (await _itemContainerRelations.WhereAsync(r => r.ItemId == iid)).FirstOrDefault();
        if (relation is null) return null;

        // Load the container and its photo(s)
        var dbContainer = await _containers.GetAsync(relation.ContainerId.ToString());
        if (dbContainer is null) return null;
        var dbPhotos = await _photos.WhereAsync(p => p.OwnerUniqueId == dbContainer.ContainerId);
        return dbContainer.ToDomain(dbPhotos);
    }

    public async Task<List<Item>> GetAllItemsWithPhotosAsync()
    {
        var items = await _items.GetAllAsync();
        var itemIds = items.Select(i => (object)i.ItemId).ToList();
        var photos = await _photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), itemIds);

        var photosByItem = GroupPhotosByOwnerUniqueId(photos);

        return MapDbItemsToDomain(items, photosByItem);
    }

    public async Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm)
    {
        // Case-insensitive search with index support using SQLite LIKE and NOCASE collation
        var pattern = $"%{searchTerm}%";
        var table = nameof(DbItem); // default table name used by sqlite-net
        var items = await _items.QueryAsync($"SELECT * FROM {table} WHERE Name LIKE ? COLLATE NOCASE", pattern);
        _logger.LogDebug("GetItemsWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, items.Count);
        var itemIds = items.Select(i => (object)i.ItemId).ToList();
        var photos = await _photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), itemIds);

        var photosByItem = GroupPhotosByOwnerUniqueId(photos);

        return MapDbItemsToDomain(items, photosByItem);
    }

    public async Task InsertContainerAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        var dbContainer = container.ToDb();
        await _containers.InsertAsync(dbContainer);
    }

    public async Task InsertItemAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var dbItem = item.ToDb();
        await _items.InsertAsync(dbItem);
    }

    public async Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(imageItem);
        var dbImage = imageItem.ToDb(ownerId);
        await _photos.InsertAsync(dbImage);
    }

    public async Task InsertItemContainerRelation(Guid itemId, Guid containerId)
    {
        var relation = new DbItemContainerRelation
        {
            ItemId = itemId,
            ContainerId = containerId
        };

        await _itemContainerRelations.InsertAsync(relation);
    }

    public async Task UpdateContainerAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        var dbContainer = container.ToDb();
        await _containers.UpdateAsync(dbContainer);
    }

    public async Task UpdateItemAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var dbItem = item.ToDb();
        await _items.UpdateAsync(dbItem);
    }

    public async Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(image);
        var dbImage = image.ToDb(ownerId);
        await _photos.UpdateAsync(dbImage);
    }

    public async Task DeleteItemAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var iid)) return;

        // Delete item images
        var images = await _photos.WhereAsync(p => p.OwnerUniqueId == iid);
        foreach (var img in images)
        {
            await _photos.DeleteAsync(img);
        }

        // Delete relations
        var relations = await _itemContainerRelations.WhereAsync(r => r.ItemId == iid);
        foreach (var rel in relations)
        {
            await _itemContainerRelations.DeleteAsync(rel);
        }

        // Delete item
        var dbItem = await _items.GetAsync(itemId);
        if (dbItem is not null)
        {
            await _items.DeleteAsync(dbItem);
        }
    }

    public async Task DeleteContainerAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return;

        // Delete container images
        var images = await _photos.WhereAsync(p => p.OwnerUniqueId == cid);
        foreach (var img in images)
        {
            await _photos.DeleteAsync(img);
        }

        // Delete relations (items remain)
        var relations = await _itemContainerRelations.WhereAsync(r => r.ContainerId == cid);
        foreach (var rel in relations)
        {
            await _itemContainerRelations.DeleteAsync(rel);
        }

        // Delete container
        var dbContainer = await _containers.GetAsync(containerId);
        if (dbContainer is not null)
        {
            await _containers.DeleteAsync(dbContainer);
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
