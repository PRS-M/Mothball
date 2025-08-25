using System.Linq.Expressions;
using CoreApp.Models;
using MothballMobile.Infrastructure.DatabaseModels;
using MothballMobile.Infrastructure.Mappers;

namespace MothballMobile.Infrastructure;

public class InventoryDomainRepository : IInventoryDomainRepository
{
    private readonly IRepository<DbContainer> _containers;
    private readonly IRepository<DbItem> _items;
    private readonly IRepository<DbPhoto> _photos;
    private readonly IRepository<DbItemContainerRelation> _itemContainerRelations;

    public InventoryDomainRepository(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbPhoto> photos,
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
        var dbPhoto = await _photos.FirstOrDefaultAsync(p => p.OwnerUniqueId == containerId);
        var photo = dbPhoto?.ToDomain();

        return db.ToDomain(photo);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetItemsForContainerAsync(string containerId)
    {
        IEnumerable<DbItemContainerRelation> dbItemContainerRelations =
            await _itemContainerRelations.WhereAsync(r => r.ContainerId == containerId);

        var itemIds = dbItemContainerRelations.Select(r => r.ItemId).ToList();

        List<DbItem> items = await _items.WhereInAsync(nameof(DbItem.UniqueId), itemIds);
        List<DbPhoto> photos = await _photos.WhereInAsync(nameof(DbPhoto.OwnerUniqueId), itemIds);
        Dictionary<string, IEnumerable<DbPhoto>> photosByItem = GroupPhotosByOwnerUniqueId(photos);

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

        var dbPhotos = await _photos.WhereAsync(p => p.OwnerUniqueId == itemId);
        return dbItem.ToDomain(dbPhotos);
    }

    public async Task<List<Item>> GetAllItemsWithPhotosAsync()
    {
        var items = await _items.GetAllAsync();
        var itemIds = items.Select(i => i.UniqueId).ToList();
        var photos = await _photos.WhereInAsync(nameof(DbPhoto.OwnerUniqueId), itemIds);

        var photosByItem = GroupPhotosByOwnerUniqueId(photos);

        return MapDbItemsToDomain(items, photosByItem);
    }

    public async Task<List<Item>> GetItemsWithPhotosAsync(Expression<Func<DbItem, bool>> predicate)
    {
        var items = await _items.WhereAsync(predicate);
        var itemIds = items.Select(i => i.UniqueId).ToList();
        var photos = await _photos.WhereInAsync(nameof(DbPhoto.OwnerUniqueId), itemIds);

        var photosByItem = GroupPhotosByOwnerUniqueId(photos);

        return MapDbItemsToDomain(items, photosByItem);
    }

    private static List<Item> MapDbItemsToDomain(List<DbItem> items, Dictionary<string, IEnumerable<DbPhoto>> photosByItem)
    {
        var domain = new List<Item>(items.Count);
        foreach (var dbItem in items)
        {
            photosByItem.TryGetValue(dbItem.UniqueId, out var dbPhotosForItem);
            domain.Add(dbItem.ToDomain(dbPhotosForItem));
        }

        return domain;
    }

    private static Dictionary<string, IEnumerable<DbPhoto>> GroupPhotosByOwnerUniqueId(List<DbPhoto> photos)
    {
        return photos
            .Where(p => p.OwnerUniqueId != null)
            .GroupBy(p => p.OwnerUniqueId!)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }
}
