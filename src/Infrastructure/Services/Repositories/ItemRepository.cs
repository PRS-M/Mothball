using System.Diagnostics;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;
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

    /// <inheritdoc />
    public async Task<Item?> GetWithPhotosAsync(string itemId)
    {
        logger.LogDebug("GetWithPhotosAsync: itemId={ItemId}", itemId);

        if (!RepositoryQueryHelpers.TryParseGuid(itemId, out Guid iid, logger, "GetWithPhotosAsync", itemId))
        {
            return null;
        }

        DbItem? dbItem = (await items.WhereAsync(item => item.ItemId == iid)).FirstOrDefault();
        if (dbItem is null) return null;

        IEnumerable<DbImage> dbPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == dbItem.ItemId);
        return dbItem.ToDomain(dbPhotos);
    }

    private Task<List<Item>> GetAllWithPhotosAsync()
        => GetItemsInternalAsync();

    private Task<List<Item>> GetAllWithPhotosAsync(int pageNumber, int pageSize)
        => GetItemsInternalAsync(pageNumber, pageSize);

    public Task<List<Item>> QueryWithPhotosAsync(ItemListSpecification specification)
    {
        var (term, hasSearch) = RepositoryQueryHelpers.NormalizeSearch(specification.SearchTerm);
        var hasPaging = RepositoryQueryHelpers.TryGetPaging(
            specification.PageNumber,
            specification.PageSize,
            out var pageNumberValue,
            out var pageSizeValue);

        if (hasSearch)
        {
            return specification.Filter switch
            {
                ItemQueryFilter.Assigned => SearchAssignedWithPhotosAsync(
                    term!,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null),
                ItemQueryFilter.Unassigned => SearchUnassignedWithPhotosAsync(
                    term!,
                    specification.ExcludedContainerId,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null),
                _ => SearchWithPhotosAsync(
                    term!,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null),
            };
        }

        if (hasPaging)
        {
            return specification.Filter switch
            {
                ItemQueryFilter.Assigned => GetAssignedWithPhotosAsync(pageNumberValue, pageSizeValue),
                ItemQueryFilter.Unassigned => GetUnassignedWithPhotosAsync(
                    pageNumberValue,
                    pageSizeValue,
                    specification.ExcludedContainerId),
                _ => GetAllWithPhotosAsync(pageNumberValue, pageSizeValue),
            };
        }

        return specification.Filter switch
        {
            ItemQueryFilter.Assigned => SearchAssignedWithPhotosAsync(string.Empty),
            ItemQueryFilter.Unassigned => SearchUnassignedWithPhotosAsync(string.Empty, specification.ExcludedContainerId),
            _ => GetAllWithPhotosAsync(),
        };
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
            .GroupBy(r => r.ItemId)
            .Select(group => group.OrderBy(r => r.Id).First())
            .OrderBy(r => r.Id);
        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            relations = relations
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue);
        }

        List<Guid> itemIds = relations.Select(r => r.ItemId).ToList();
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

    private async Task<List<Item>> GetUnassignedWithPhotosAsync(
        int pageNumber,
        int pageSize,
        Guid? excludedContainerId = null)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);

        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        List<DbItem> unassigned;
        if (excludedContainerId is Guid containerId)
        {
            unassigned = await items.QueryAsync(
                $"SELECT * FROM {nameof(DbItem)} " +
                $"WHERE COALESCE((SELECT TotalQuantity FROM {nameof(DbItemInventory)} inv WHERE inv.ItemId = {nameof(DbItem)}.ItemId), 1) > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} " +
                $"WHERE {nameof(DbItemContainerRelation)}.ItemId = {nameof(DbItem)}.ItemId AND Quantity > 0), 0) " +
                $"AND NOT EXISTS (SELECT 1 FROM {nameof(DbItemContainerRelation)} r " +
                $"WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.ContainerId = ? AND r.Quantity > 0) " +
                $"ORDER BY Name COLLATE NOCASE " +
                $"LIMIT ? OFFSET ?",
                containerId,
                pageSize,
                offset);
        }
        else
        {
            unassigned = await items.QueryAsync(
                $"SELECT * FROM {nameof(DbItem)} " +
                $"WHERE COALESCE((SELECT TotalQuantity FROM {nameof(DbItemInventory)} inv WHERE inv.ItemId = {nameof(DbItem)}.ItemId), 1) > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} " +
                $"WHERE {nameof(DbItemContainerRelation)}.ItemId = {nameof(DbItem)}.ItemId AND Quantity > 0), 0) " +
                $"ORDER BY Name COLLATE NOCASE " +
                $"LIMIT ? OFFSET ?",
                pageSize,
                offset);
        }

        return unassigned.Count == 0 ? [] : await MapItemsWithPhotosAsync(unassigned);
    }

    private async Task<List<Item>> GetAssignedWithPhotosAsync(int pageNumber, int pageSize)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);

        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        List<DbItem> assigned = await items.QueryAsync(
            $"SELECT * FROM {nameof(DbItem)} " +
            $"WHERE COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} " +
            $"WHERE {nameof(DbItemContainerRelation)}.ItemId = {nameof(DbItem)}.ItemId AND Quantity > 0), 0) > 0 " +
            $"ORDER BY Name COLLATE NOCASE " +
            $"LIMIT ? OFFSET ?",
            pageSize,
            offset);

        return assigned.Count == 0 ? [] : await MapItemsWithPhotosAsync(assigned);
    }

    private async Task<List<Item>> SearchWithPhotosAsync(
        string searchTerm,
        int? pageNumber = null,
        int? pageSize = null)
    {
        string pattern = $"%{searchTerm}%";
        List<DbItem> itemsQuery;

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            itemsQuery = await items.QueryAsync(
                $"SELECT * FROM {nameof(DbItem)} WHERE Name LIKE ? COLLATE NOCASE ORDER BY rowid LIMIT ? OFFSET ?",
                pattern,
                pageSizeValue,
                RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue));
        }
        else
        {
            itemsQuery = await items.QueryAsync(
                $"SELECT * FROM {nameof(DbItem)} WHERE Name LIKE ? COLLATE NOCASE ORDER BY rowid",
                pattern);
        }

        logger.LogDebug("SearchWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);
        return await MapItemsWithPhotosAsync(itemsQuery);
    }

    private async Task<List<Item>> SearchUnassignedWithPhotosAsync(
        string searchTerm,
        Guid? excludedContainerId = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        string pattern = $"%{searchTerm}%";
        List<DbItem> itemsQuery;

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            if (excludedContainerId is Guid containerId)
            {
                itemsQuery = await items.QueryAsync(
                    $@"SELECT * FROM {nameof(DbItem)}
                                     WHERE COALESCE((SELECT TotalQuantity FROM {nameof(DbItemInventory)} inv WHERE inv.ItemId = {nameof(DbItem)}.ItemId), 1) > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} r
                                             WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.Quantity > 0), 0)
                         AND Name LIKE ? COLLATE NOCASE
                         AND NOT EXISTS (SELECT 1 FROM {nameof(DbItemContainerRelation)} xr
                                         WHERE xr.ItemId = {nameof(DbItem)}.ItemId
                                           AND xr.ContainerId = ?
                                           AND xr.Quantity > 0)
                       ORDER BY Name COLLATE NOCASE
                       LIMIT ? OFFSET ?",
                    pattern,
                    containerId,
                    pageSizeValue,
                    RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue));
            }
            else
            {
                itemsQuery = await items.QueryAsync(
                    $@"SELECT * FROM {nameof(DbItem)}
                                     WHERE COALESCE((SELECT TotalQuantity FROM {nameof(DbItemInventory)} inv WHERE inv.ItemId = {nameof(DbItem)}.ItemId), 1) > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} r
                                             WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.Quantity > 0), 0)
                         AND Name LIKE ? COLLATE NOCASE
                       ORDER BY Name COLLATE NOCASE
                       LIMIT ? OFFSET ?",
                    pattern,
                    pageSizeValue,
                    RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue));
            }
        }
        else
        {
            if (excludedContainerId is Guid containerId)
            {
                itemsQuery = await items.QueryAsync(
                    $@"SELECT * FROM {nameof(DbItem)}
                                     WHERE COALESCE((SELECT TotalQuantity FROM {nameof(DbItemInventory)} inv WHERE inv.ItemId = {nameof(DbItem)}.ItemId), 1) > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} r
                                             WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.Quantity > 0), 0)
                         AND Name LIKE ? COLLATE NOCASE
                         AND NOT EXISTS (SELECT 1 FROM {nameof(DbItemContainerRelation)} xr
                                         WHERE xr.ItemId = {nameof(DbItem)}.ItemId
                                           AND xr.ContainerId = ?
                                           AND xr.Quantity > 0)
                       ORDER BY Name COLLATE NOCASE",
                    pattern,
                    containerId);
            }
            else
            {
                itemsQuery = await items.QueryAsync(
                    $@"SELECT * FROM {nameof(DbItem)}
                                     WHERE COALESCE((SELECT TotalQuantity FROM {nameof(DbItemInventory)} inv WHERE inv.ItemId = {nameof(DbItem)}.ItemId), 1) > COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} r
                                             WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.Quantity > 0), 0)
                         AND Name LIKE ? COLLATE NOCASE
                       ORDER BY Name COLLATE NOCASE",
                    pattern);
            }
        }

        logger.LogDebug("SearchUnassignedWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);
        return await MapItemsWithPhotosAsync(itemsQuery);
    }

    private async Task<List<Item>> SearchAssignedWithPhotosAsync(
        string searchTerm,
        int? pageNumber = null,
        int? pageSize = null)
    {
        string pattern = $"%{searchTerm}%";
        List<DbItem> itemsQuery;

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            itemsQuery = await items.QueryAsync(
                $@"SELECT * FROM {nameof(DbItem)}
                   WHERE COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} r
                                   WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.Quantity > 0), 0) > 0
                     AND Name LIKE ? COLLATE NOCASE
                   ORDER BY Name COLLATE NOCASE
                   LIMIT ? OFFSET ?",
                pattern,
                pageSizeValue,
                RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue));
        }
        else
        {
            itemsQuery = await items.QueryAsync(
                $@"SELECT * FROM {nameof(DbItem)}
                   WHERE COALESCE((SELECT SUM(Quantity) FROM {nameof(DbItemContainerRelation)} r
                                   WHERE r.ItemId = {nameof(DbItem)}.ItemId AND r.Quantity > 0), 0) > 0
                     AND Name LIKE ? COLLATE NOCASE
                   ORDER BY Name COLLATE NOCASE",
                pattern);
        }

        logger.LogDebug("SearchAssignedWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);
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

        // Query items that match search AND belong to the container, collapsed to aggregate rows.
        List<DbItem> itemsQuery = await items.QueryAsync(
            $@"SELECT i.* FROM {nameof(DbItem)} i
               INNER JOIN {nameof(DbItemContainerRelation)} r ON i.ItemId = r.ItemId
               WHERE r.ContainerId = ? AND r.Quantity > 0 AND i.Name LIKE ? COLLATE NOCASE
               GROUP BY i.ItemId
               ORDER BY MIN(r.Id)
               LIMIT ? OFFSET ?",
            cid, pattern, pageSize, offset);

        var result = await MapItemsWithPhotosAsync(itemsQuery);
        sw.Stop();

        logger.LogInformation(
            "SearchItemsInContainerAsync: containerId={ContainerId}, term='{SearchTerm}', page={PageNumber}, size={PageSize}, matched={Count}, elapsedMs={Elapsed}",
            containerId, searchTerm, pageNumber, pageSize, result.Count, sw.ElapsedMilliseconds);

        return result;
    }

    /// <inheritdoc />
    public async Task InsertAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await items.InsertAsync(item.ToDb());
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await items.UpdateAsync(item.ToDb());
    }

    /// <inheritdoc />
    public async Task DeletePhotoAsync(Item item, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(item);

        await transactionRunner.RunAsync(scope =>
        {
            scope.DeleteImage(imageId, item.ItemId);
            scope.UpdateItem(item.ToDb());
        });
    }

    /// <inheritdoc />
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

    private async Task<List<Item>> LoadItemsWithPhotosByIdsAsync(List<Guid> itemIds)
    {
        if (itemIds.Count == 0) return [];

        List<DbItem> itemsList = await items.WhereInAsync(
            nameof(DbItem.ItemId),
            itemIds.Select(itemId => (object)itemId).ToList());
        var itemsById = (await MapItemsWithPhotosAsync(itemsList))
            .ToDictionary(item => item.ItemId);

        return itemIds
            .Where(itemsById.ContainsKey)
            .Select(itemId => itemsById[itemId])
            .ToList();
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
        return dbItems.Select(dbItem =>
        {
            photosByItem.TryGetValue(dbItem.ItemId, out var itemPhotos);
            return dbItem.ToDomain(itemPhotos);
        }).ToList();
    }

    #endregion
}
