using System.Linq.Expressions;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using Infrastructure.Interfaces;
using MothballMobile.Infrastructure.DatabaseModels;
using MothballMobile.Infrastructure.Mappers;

namespace MothballMobile.Infrastructure;

public class InventoryDomainRepository : IInventoryDomainRepository
{
    private readonly IRepository<DbContainer> _containers;
    private readonly IRepository<DbItem> _items;
    private readonly IRepository<DbImage> _photos;
    private readonly IRepository<DbItemContainerRelation> _itemContainerRelations;

    public InventoryDomainRepository(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations)
    {
        _containers = containers;
        _items = items;
        _photos = photos;
        _itemContainerRelations = itemContainerRelations;
    }

    /// <inheritdoc />
    public async Task<Container?> GetContainerAsync(string containerId)
    {
        var db = await _containers.GetAsync(containerId);
        if (db is null) return null;

        // Load container photo if any
        var dbPhotos = await _photos.WhereAsync(p => p.OwnerUniqueId.ToString() == containerId);
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
        IEnumerable<DbItemContainerRelation> dbItemContainerRelations =
            await _itemContainerRelations.WhereAsync(r => r.ContainerId.ToString() == containerId);

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
        var dbItem = await _items.GetAsync(itemId);
        if (dbItem is null) return null;

        var dbPhotos = await _photos.WhereAsync(p => p.OwnerUniqueId.ToString() == itemId);
        return dbItem.ToDomain(dbPhotos);
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
        var items = await _items.WhereAsync(i => i.Name.Contains(searchTerm));
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
