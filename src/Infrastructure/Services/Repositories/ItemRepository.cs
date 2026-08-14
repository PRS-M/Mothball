using System.Diagnostics;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;
using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly ITransactionRunner transactionRunner;
    private readonly IRepository<DbItem> items;
    private readonly IRepository<DbImage> photos;
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;
    private readonly ILogger<ItemRepository> logger;

    public ItemRepository(
        ITransactionRunner transactionRunner,
        IRepository<DbItem> items,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        ILogger<ItemRepository> logger)
    {
        this.transactionRunner = transactionRunner;
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
        IEnumerable<DbItemContainerRelation> relations = await itemContainerRelations.WhereAsync(r => r.ItemId == dbItem.ItemId);
        return MapItem(dbItem, dbPhotos, relations);
    }

    private Task<List<Item>> GetAllWithPhotosAsync()
        => GetItemsInternalAsync();

    private Task<List<Item>> GetAllWithPhotosAsync(int pageNumber, int pageSize)
        => GetItemsInternalAsync(pageNumber, pageSize);

    public Task<List<Item>> QueryWithPhotosAsync(ItemListSpecification specification)
    {
        var (term, hasSearch) = RepositoryQueryHelpers.NormalizeSearch(specification.SearchTerm);

        if (hasSearch)
        {
            return specification.Filter == ItemQueryFilter.Unassigned
                ? SearchUnassignedWithPhotosAsync(term!)
                : SearchWithPhotosAsync(term!);
        }

        if (RepositoryQueryHelpers.TryGetPaging(specification.PageNumber, specification.PageSize, out var pageNumberValue, out var pageSizeValue))
        {
            return specification.Filter == ItemQueryFilter.Unassigned
                ? GetUnassignedWithPhotosAsync(pageNumberValue, pageSizeValue)
                : GetAllWithPhotosAsync(pageNumberValue, pageSizeValue);
        }

        return specification.Filter == ItemQueryFilter.Unassigned
            ? SearchUnassignedWithPhotosAsync(string.Empty)
            : GetAllWithPhotosAsync();
    }

    public Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification)
    {
        var (term, hasSearch) = RepositoryQueryHelpers.NormalizeSearch(specification.SearchTerm);
        var hasPaging = RepositoryQueryHelpers.TryGetPaging(
            specification.PageNumber,
            specification.PageSize,
            out var pageNumberValue,
            out var pageSizeValue);

        if (hasSearch)
        {
            return SearchItemsInContainerAsync(
                specification.ContainerId,
                term!,
                hasPaging ? pageNumberValue : 0,
                hasPaging ? pageSizeValue : int.MaxValue);
        }

        return GetItemsForContainerAsync(
            specification.ContainerId,
            hasPaging ? pageNumberValue : null,
            hasPaging ? pageSizeValue : null);
    }

    private async Task<List<Item>> GetItemsForContainerAsync(string containerId, int? pageNumber = null, int? pageSize = null)
    {
        if (!RepositoryQueryHelpers.TryParseGuid(containerId, out Guid cid, logger, "GetItemsForContainerAsync", containerId))
        {
            return [];
        }

        var sw = Stopwatch.StartNew();
        IEnumerable<DbItemContainerRelation> relations = (await itemContainerRelations.WhereAsync(r => r.ContainerId == cid))
            .Where(r => r.Quantity > 0)
            .OrderBy(r => r.Id);
        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            relations = relations
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue);
        }

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

    private async Task<List<Item>> GetUnassignedWithPhotosAsync(int pageNumber, int pageSize)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);

        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        // NOTE: no uniqueness constraints are enforced; an item may have multiple relations.
        // This query treats any presence in the relation table as "assigned".
        List<DbItem> unassigned = await items.QueryAsync(
            $"SELECT * FROM {nameof(DbItem)} " +
            $"WHERE TotalQuantity > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} " +
            $"WHERE {nameof(DbItemContainerRelation)}.ItemId = {nameof(DbItem)}.ItemId AND Quantity > 0), 0) " +
            $"ORDER BY Name COLLATE NOCASE " +
            $"LIMIT ? OFFSET ?",
            pageSize,
            offset);

        return unassigned.Count == 0 ? [] : await MapItemsWithPhotosAsync(unassigned);
    }

    private async Task<List<Item>> SearchWithPhotosAsync(string searchTerm)
    {
        string pattern = $"%{searchTerm}%";
        List<DbItem> itemsQuery = await items.QueryAsync(
            $"SELECT * FROM {nameof(DbItem)} WHERE Name LIKE ? COLLATE NOCASE ORDER BY rowid",
            pattern);

        logger.LogDebug("SearchWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);
        return await MapItemsWithPhotosAsync(itemsQuery);
    }

    private async Task<List<Item>> SearchUnassignedWithPhotosAsync(string searchTerm)
    {
        string pattern = $"%{searchTerm}%";

        List<DbItem> itemsQuery = await items.QueryAsync(
            $@"SELECT * FROM {nameof(DbItem)}
                             WHERE TotalQuantity > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} r
                                     WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.Quantity > 0), 0)
                 AND Name LIKE ? COLLATE NOCASE
               ORDER BY Name COLLATE NOCASE",
            pattern);

        logger.LogDebug("SearchUnassignedWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);
        return await MapItemsWithPhotosAsync(itemsQuery);
    }

    private async Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
    {
        if (!RepositoryQueryHelpers.TryParseGuid(containerId, out Guid cid, logger, "SearchItemsInContainerAsync", containerId))
        {
            return [];
        }

        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);
        string pattern = $"%{searchTerm}%";

        var sw = Stopwatch.StartNew();

        // Query items that match search AND belong to the container
        List<DbItem> itemsQuery = await items.QueryAsync(
            $@"SELECT i.* FROM {nameof(DbItem)} i
               INNER JOIN {nameof(DbItemContainerRelation)} r ON i.ItemId = r.ItemId
               WHERE r.ContainerId = ? AND i.Name LIKE ? COLLATE NOCASE
               ORDER BY r.Id
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

    public async Task DeletePhotoAsync(Item item, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(item);

        await transactionRunner.RunAsync(scope =>
        {
            scope.DeleteImage(imageId, item.ItemId);
            scope.UpdateItem(item.ToDb());
        });
    }

    public async Task DeleteAsync(string itemId)
    {
        if (!RepositoryQueryHelpers.TryParseGuid(itemId, out Guid iid, logger)) return;

        await transactionRunner.RunAsync(scope =>
        {
            scope.DeleteImagesByOwner(iid);
            scope.DeleteRelationsByItem(iid);
            scope.DeleteItem(iid);
        });
    }

    #region Private Helpers - Data Loading

    private async Task<List<Item>> GetItemsInternalAsync(int? pageNumber = null, int? pageSize = null)
    {
        List<DbItem> dbItems = await RepositoryQueryHelpers.QueryAllOrderedByRowIdAsync(
            items,
            pageNumber,
            pageSize);

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
        Dictionary<Guid, IEnumerable<DbImage>> photosByItem =
            await RepositoryQueryHelpers.LoadLookupByIdsAsync(
                photos,
                nameof(DbImage.OwnerUniqueId),
                itemIds,
                p => p.OwnerUniqueId);
        Dictionary<Guid, IEnumerable<DbItemContainerRelation>> relationsByItem =
            await RepositoryQueryHelpers.LoadLookupByIdsAsync(
                itemContainerRelations,
                nameof(DbItemContainerRelation.ItemId),
                itemIds,
                relation => relation.ItemId);

        return dbItems.Select(dbItem =>
        {
            photosByItem.TryGetValue(dbItem.ItemId, out var itemPhotos);
            relationsByItem.TryGetValue(dbItem.ItemId, out var itemRelations);
            return MapItem(dbItem, itemPhotos, itemRelations);
        }).ToList();
    }

    private static Item MapItem(
        DbItem dbItem,
        IEnumerable<DbImage>? dbPhotos,
        IEnumerable<DbItemContainerRelation>? relations = null)
    {
        var item = dbItem.ToDomain(dbPhotos);
        item.SetAssignedQuantity(relations?.Where(relation => relation.Quantity > 0).Sum(relation => relation.Quantity) ?? 0);
        return item;
    }

    #endregion
}
