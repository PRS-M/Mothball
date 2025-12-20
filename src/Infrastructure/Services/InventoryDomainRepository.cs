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
        DbContainer? dbContainer = await containers.GetAsync(containerId);
        if (dbContainer is null) return null;

        // Load container photo(s) and relations (for item counts/quantities)
        var dbPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == dbContainer.ContainerId);
        var relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == dbContainer.ContainerId);
        return dbContainer.ToDomain(dbPhotos, relations);
    }

    /// <inheritdoc />
    public async Task<List<Container>> GetAllContainersAsync()
    {
        List<DbContainer> dbContainers = await containers.GetAllAsync();
        return await MapContainersWithPhotosAndRelationsAsync(dbContainers);
    }

    public async Task<List<Container>> GetAllContainersAsync(int pageNumber, int pageSize)
    {
        ValidatePaging(pageNumber, pageSize);

        // Zero-based page index
        int offset = pageNumber * pageSize;
        List<DbContainer> dbContainers = await containers.GetAllAsync(offset, pageSize);
        return await MapContainersWithPhotosAndRelationsAsync(dbContainers);
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
        return await LoadItemsWithPhotosByIdsAsync(itemIds);
    }

    /// <inheritdoc />
    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId)
    {
        logger.LogDebug("GetContainerWithItemsAndPhotosAsync: containerId={ContainerId}", containerId);

        if (!Guid.TryParse(containerId, out var cid))
        {
            return null;
        }

        DbContainer? dbContainer = await containers.GetAsync(containerId);
        if (dbContainer is null) return null;

        var dbContainerPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == dbContainer.ContainerId);
        var relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == cid);
        var container = dbContainer.ToDomain(dbContainerPhotos, relations);

        var itemIds = relations.Select(r => (object)r.ItemId).ToList();
        if (itemIds.Count == 0)
        {
            return (container, new List<Item>());
        }

        var itemsList = await items.WhereInAsync(nameof(DbItem.ItemId), itemIds);
        var photosForItems = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), itemIds);
        var photosByItem = GroupPhotosByOwnerUniqueId(photosForItems);

        return (container, MapDbItemsToDomain(itemsList, photosByItem));
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

        // Include relations to keep ItemCount consistent if the container is displayed.
        var relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == dbContainer.ContainerId);
        return dbContainer.ToDomain(dbPhotos, relations);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetAllItemsWithPhotosAsync()
    {
        var itemsAll = await items.GetAllAsync();
        return await MapItemsWithPhotosAsync(itemsAll);
    }

    public async Task<List<Item>> GetAllItemsWithPhotosAsync(int pageNumber, int pageSize)
    {
        ValidatePaging(pageNumber, pageSize);

        // Zero-based page index
        int offset = pageNumber * pageSize;
        List<DbItem> itemsAll = await items.GetAllAsync(offset, pageSize);
        return await MapItemsWithPhotosAsync(itemsAll);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetUnassignedItemsWithPhotosAsync(int pageNumber, int pageSize)
    {
        ValidatePaging(pageNumber, pageSize);

        int offset = pageNumber * pageSize;
        var itemTable = nameof(DbItem);
        var relTable = nameof(DbItemContainerRelation);

        // NOTE: no uniqueness constraints are enforced; an item may have multiple relations.
        // This query treats any presence in the relation table as "assigned".
        var unassigned = await items.QueryAsync(
            $"SELECT * FROM {itemTable} " +
            $"WHERE ItemId NOT IN (SELECT ItemId FROM {relTable}) " +
            $"ORDER BY Name COLLATE NOCASE " +
            $"LIMIT ? OFFSET ?",
            pageSize,
            offset);

        var itemIds = unassigned.Select(i => (object)i.ItemId).ToList();
        if (itemIds.Count == 0)
        {
            return new List<Item>();
        }

        return await MapItemsWithPhotosAsync(unassigned);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm)
    {
        // Case-insensitive search with index support using SQLite LIKE and NOCASE collation
        var pattern = $"%{searchTerm}%";
        var table = nameof(DbItem); // default table name used by sqlite-net
        var itemsQuery = await items.QueryAsync($"SELECT * FROM {table} WHERE Name LIKE ? COLLATE NOCASE", pattern);
        logger.LogDebug("GetItemsWithPhotosAsync: term='{SearchTerm}', matched={Count}", searchTerm, itemsQuery.Count);

        return await MapItemsWithPhotosAsync(itemsQuery);
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
    public async Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var relation = new DbItemContainerRelation
        {
            ItemId = itemId,
            ContainerId = containerId,
            Quantity = quantity,
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

    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 0) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 0.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");
    }

    private async Task<List<Item>> LoadItemsWithPhotosByIdsAsync(List<object> itemIds)
    {
        if (itemIds.Count == 0) return new List<Item>();

        List<DbItem> itemsList = await items.WhereInAsync(nameof(DbItem.ItemId), itemIds);
        return await MapItemsWithPhotosAsync(itemsList);
    }

    private async Task<List<Item>> MapItemsWithPhotosAsync(List<DbItem> dbItems)
    {
        if (dbItems.Count == 0) return new List<Item>();

        List<object> itemIds = dbItems.Select(i => (object)i.ItemId).ToList();
        Dictionary<Guid, IEnumerable<DbImage>> photosByItem = await LoadPhotosByOwnerIdsAsync(itemIds);
        return MapDbItemsToDomain(dbItems, photosByItem);
    }

    private async Task<List<Container>> MapContainersWithPhotosAndRelationsAsync(List<DbContainer> dbContainers)
    {
        if (dbContainers.Count == 0)
        {
            return new List<Container>();
        }

        List<object> containerIds = [.. dbContainers.Select(c => (object)c.ContainerId)];
        Dictionary<Guid, IEnumerable<DbImage>> photosByContainer = await LoadPhotosByOwnerIdsAsync(containerIds);
        Dictionary<Guid, IEnumerable<DbItemContainerRelation>> relByContainer = await LoadRelationsByContainerIdsAsync(containerIds);

        var mapped = new List<Container>(dbContainers.Count);
        foreach (var dbContainer in dbContainers)
        {
            photosByContainer.TryGetValue(dbContainer.ContainerId, out var dbPhotosForContainer);
            relByContainer.TryGetValue(dbContainer.ContainerId, out var relsForContainer);
            mapped.Add(dbContainer.ToDomain(dbPhotosForContainer, relsForContainer));
        }

        return mapped;
    }

    private async Task<Dictionary<Guid, IEnumerable<DbItemContainerRelation>>> LoadRelationsByContainerIdsAsync(List<object> containerIds)
    {
        if (containerIds.Count == 0) return new Dictionary<Guid, IEnumerable<DbItemContainerRelation>>();

        List<DbItemContainerRelation> relations = await itemContainerRelations.WhereInAsync(nameof(DbItemContainerRelation.ContainerId), containerIds);
        return relations
            .GroupBy(r => r.ContainerId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    private async Task<Dictionary<Guid, IEnumerable<DbImage>>> LoadPhotosByOwnerIdsAsync(List<object> ownerIds)
    {
        if (ownerIds.Count == 0) return new Dictionary<Guid, IEnumerable<DbImage>>();

        List<DbImage> photosList = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), ownerIds);
        return GroupPhotosByOwnerUniqueId(photosList);
    }

    private static Dictionary<Guid, IEnumerable<DbImage>> GroupPhotosByOwnerUniqueId(List<DbImage> photos)
    {
        return photos
            .GroupBy(p => p.OwnerUniqueId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }
}
