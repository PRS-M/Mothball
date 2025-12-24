using System.Diagnostics;
using System.Linq.Expressions;
using CoreApp.Entities.ItemAggregate;
using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ItemRepository : IItemRepository
{
    private readonly IRepository<DbItem> items;
    private readonly IRepository<DbImage> photos;
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;
    private readonly ILogger<ItemRepository> logger;

    public ItemRepository(
        IRepository<DbItem> items,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        ILogger<ItemRepository> logger)
    {
        this.items = items;
        this.photos = photos;
        this.itemContainerRelations = itemContainerRelations;
        this.logger = logger;
    }

    public async Task<Item?> GetWithPhotosAsync(string itemId)
    {
        logger.LogDebug("GetWithPhotosAsync: itemId={ItemId}", itemId);

        DbItem? dbItem = await items.GetAsync(itemId);
        if (dbItem is null) return null;

        IEnumerable<DbImage> dbPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == dbItem.ItemId);
        return dbItem.ToDomain(dbPhotos);
    }

    public Task<List<Item>> GetAllWithPhotosAsync()
        => GetItemsInternalAsync();

    public Task<List<Item>> GetAllWithPhotosAsync(int pageNumber, int pageSize)
        => GetItemsInternalAsync(pageNumber, pageSize);

    public async Task<List<Item>> GetItemsForContainerAsync(string containerId)
    {
        if (!TryParseGuid(containerId, out Guid cid, "GetItemsForContainerAsync", containerId))
        {
            return [];
        }

        var sw = Stopwatch.StartNew();
        IEnumerable<DbItemContainerRelation> relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == cid);
        List<object> itemIds = relations.Select(r => (object)r.ItemId).ToList();
        var itemsWithPhotos = await LoadItemsWithPhotosByIdsAsync(itemIds);
        sw.Stop();

        logger.LogInformation(
            "GetItemsForContainerAsync: containerId={ContainerId}, relations={RelationCount}, itemsLoaded={ItemsCount}, elapsedMs={Elapsed}",
            containerId,
            itemIds.Count,
            itemsWithPhotos.Count,
            sw.ElapsedMilliseconds);

        return itemsWithPhotos;
    }

    public async Task<List<Item>> GetByIdsWithPhotosAsync(IEnumerable<Guid> itemIds)
    {
        var ids = itemIds?.ToList() ?? new List<Guid>();
        if (ids.Count == 0) return [];

        List<object> idObjects = ids.Select(id => (object)id).ToList();
        var sw = Stopwatch.StartNew();
        var itemsWithPhotos = await LoadItemsWithPhotosByIdsAsync(idObjects);
        sw.Stop();

        logger.LogInformation(
            "GetByIdsWithPhotosAsync: ids={IdCount}, itemsLoaded={ItemsCount}, elapsedMs={Elapsed}",
            ids.Count,
            itemsWithPhotos.Count,
            sw.ElapsedMilliseconds);

        return itemsWithPhotos;
    }

    public async Task<List<Item>> GetUnassignedWithPhotosAsync(int pageNumber, int pageSize)
    {
        ValidatePaging(pageNumber, pageSize);

        int offset = CalculateOffset(pageNumber, pageSize);

        // NOTE: no uniqueness constraints are enforced; an item may have multiple relations.
        // This query treats any presence in the relation table as "assigned".
        List<DbItem> unassigned = await items.QueryAsync(
            $"SELECT * FROM {nameof(DbItem)} " +
            $"WHERE ItemId NOT IN (SELECT ItemId FROM {nameof(DbItemContainerRelation)}) " +
            $"ORDER BY Name COLLATE NOCASE " +
            $"LIMIT ? OFFSET ?",
            pageSize,
            offset);

        return unassigned.Count == 0 ? [] : await MapItemsWithPhotosAsync(unassigned);
    }

    public async Task<List<Item>> SearchWithPhotosAsync(string searchTerm)
    {
        string pattern = $"%{searchTerm}%";
        List<DbItem> itemsQuery = await items.QueryAsync(
            $"SELECT * FROM {nameof(DbItem)} WHERE Name LIKE ? COLLATE NOCASE",
            pattern);

        logger.LogDebug("SearchWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);
        return await MapItemsWithPhotosAsync(itemsQuery);
    }

    public async Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
    {
        if (!TryParseGuid(containerId, out Guid cid, "SearchItemsInContainerAsync", containerId))
        {
            return [];
        }

        ValidatePaging(pageNumber, pageSize);
        int offset = CalculateOffset(pageNumber, pageSize);
        string pattern = $"%{searchTerm}%";

        var sw = Stopwatch.StartNew();

        // Query items that match search AND belong to the container
        List<DbItem> itemsQuery = await items.QueryAsync(
            $@"SELECT i.* FROM {nameof(DbItem)} i
               INNER JOIN {nameof(DbItemContainerRelation)} r ON i.ItemId = r.ItemId
               WHERE r.ContainerId = ? AND i.Name LIKE ? COLLATE NOCASE
               LIMIT ? OFFSET ?",
            cid, pattern, pageSize, offset);

        var result = await MapItemsWithPhotosAsync(itemsQuery);
        sw.Stop();

        logger.LogInformation(
            "SearchItemsInContainerAsync: containerId={ContainerId}, term='{SearchTerm}', page={PageNumber}, size={PageSize}, matched={Count}, elapsedMs={Elapsed}",
            containerId, searchTerm, pageNumber, pageSize, result.Count, sw.ElapsedMilliseconds);

        return result;
    }

    public async Task InsertAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await items.InsertAsync(item.ToDb());
    }

    public async Task UpdateAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await items.UpdateAsync(item.ToDb());
    }

    public async Task DeleteAsync(string itemId)
    {
        if (!TryParseGuid(itemId, out Guid iid)) return;

        await DeletePhotosForOwnerAsync(iid);
        await DeleteRelationsAsync(r => r.ItemId == iid);

        DbItem? dbItem = await items.GetAsync(itemId);
        if (dbItem is not null)
        {
            await items.DeleteAsync(dbItem);
        }
    }

    #region Private Helpers - Data Loading

    private async Task<List<Item>> GetItemsInternalAsync(int? pageNumber = null, int? pageSize = null)
    {
        List<DbItem> dbItems;

        if (pageNumber.HasValue && pageSize.HasValue)
        {
            ValidatePaging(pageNumber.Value, pageSize.Value);
            int offset = CalculateOffset(pageNumber.Value, pageSize.Value);
            dbItems = await items.GetAllAsync(offset, pageSize.Value);
        }
        else
        {
            dbItems = await items.GetAllAsync();
        }

        return await MapItemsWithPhotosAsync(dbItems);
    }

    private async Task<List<Item>> LoadItemsWithPhotosByIdsAsync(List<object> itemIds)
    {
        if (itemIds.Count == 0) return [];

        List<DbItem> itemsList = await items.WhereInAsync(nameof(DbItem.ItemId), itemIds);
        return await MapItemsWithPhotosAsync(itemsList);
    }

    #endregion

    #region Private Helpers - Mapping

    private async Task<List<Item>> MapItemsWithPhotosAsync(List<DbItem> dbItems)
    {
        if (dbItems.Count == 0) return [];

        List<object> itemIds = dbItems.Select(i => (object)i.ItemId).ToList();
        Dictionary<Guid, IEnumerable<DbImage>> photosByItem = await LoadPhotosByOwnerIdsAsync(itemIds);

        return dbItems.Select(dbItem =>
        {
            photosByItem.TryGetValue(dbItem.ItemId, out var itemPhotos);
            return dbItem.ToDomain(itemPhotos);
        }).ToList();
    }

    private async Task<Dictionary<Guid, IEnumerable<DbImage>>> LoadPhotosByOwnerIdsAsync(List<object> ownerIds)
    {
        if (ownerIds.Count == 0) return [];

        List<DbImage> photosList = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), ownerIds);
        return GroupByKey(photosList, p => p.OwnerUniqueId);
    }

    private static Dictionary<TKey, IEnumerable<T>> GroupByKey<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> keySelector) where TKey : notnull
        => items.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.AsEnumerable());

    #endregion

    #region Private Helpers - CRUD

    private async Task DeletePhotosForOwnerAsync(Guid ownerId)
    {
        IEnumerable<DbImage> images = await photos.WhereAsync(p => p.OwnerUniqueId == ownerId);
        foreach (DbImage img in images)
        {
            await photos.DeleteAsync(img);
        }
    }

    private async Task DeleteRelationsAsync(Expression<Func<DbItemContainerRelation, bool>> predicate)
    {
        IEnumerable<DbItemContainerRelation> relations = await itemContainerRelations.WhereAsync(predicate);
        foreach (DbItemContainerRelation rel in relations)
        {
            await itemContainerRelations.DeleteAsync(rel);
        }
    }

    #endregion

    #region Private Helpers - Validation & Utilities

    private bool TryParseGuid(string value, out Guid result, string? methodName = null, string? logValue = null)
    {
        if (Guid.TryParse(value, out result)) return true;

        if (methodName is not null)
        {
            logger.LogWarning("{MethodName}: invalid GUID format: {Value}", methodName, logValue ?? value);
        }

        return false;
    }

    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
    }

    private static int CalculateOffset(int pageNumber, int pageSize) => pageNumber * pageSize;

    #endregion
}
