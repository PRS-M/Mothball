using CoreApp.Models;
using MothballMobile.Infrastructure.DatabaseModels;
using MothballMobile.Infrastructure.Mappers;

namespace MothballMobile.Infrastructure;

public class InventoryDomainRepository : IInventoryDomainRepository
{
    private readonly IRepository<DbContainer> _containers;
    private readonly IRepository<DbItem> _items;
    private readonly IRepository<DbPhoto> _photos;

    public InventoryDomainRepository(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbPhoto> photos)
    {
        _containers = containers;
        _items = items;
        _photos = photos;
    }

    public async Task<Container?> GetContainerAsync(string containerId)
    {
        var db = await _containers.GetAsync(containerId);
        if (db is null) return null;

        // Load container photo if any
        var photo = (await _photos.Table()
            .Where(p => p.ContainerId == containerId)
            .FirstOrDefaultAsync())
            ?.ToDomain();

        return db.ToDomain(photo);
    }

    public async Task<List<Item>> GetItemsForContainerAsync(string containerId)
    {
        var items = await _items.Table()
            .Where(i => i.ContainerId == containerId)
            .ToListAsync();

        var itemIds = items.Select(i => i.UniqueId).ToList();
        var photos = await _photos.Table().Where(p => itemIds.Contains(p.ItemId!)).ToListAsync();

        var photosByItem = photos.GroupBy(p => p.ItemId!).ToDictionary(g => g.Key, g => g.AsEnumerable());
        var domain = new List<Item>(items.Count);
        foreach (var dbItem in items)
        {
            photosByItem.TryGetValue(dbItem.UniqueId, out var dbPhotosForItem);
            domain.Add(dbItem.ToDomain(dbPhotosForItem));
        }
        return domain;
    }

    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId)
    {
        var container = await GetContainerAsync(containerId);
        if (container is null) return null;
        var items = await GetItemsForContainerAsync(containerId);
        return (container, items);
    }

    public async Task<Item?> GetItemWithPhotosAsync(string itemId)
    {
        var dbItem = await _items.GetAsync(itemId);
        if (dbItem is null) return null;

        var dbPhotos = await _photos.Table().Where(p => p.ItemId == itemId).ToListAsync();
        return dbItem.ToDomain(dbPhotos);
    }
}
