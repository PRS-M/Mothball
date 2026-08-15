using System.Diagnostics;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Specifications;
using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Microsoft.Extensions.Logging;
using CoreApp.Contracts;

namespace Infrastructure.Services.Repositories;

public class ContainerRepository : IContainerRepository
{
    private readonly ITransactionRunner transactionRunner;
    private readonly IRepository<DbContainer> containers;
    private readonly IRepository<DbImage> photos;
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;
    private readonly ILogger<ContainerRepository> logger;

    public ContainerRepository(
        ITransactionRunner transactionRunner,
        IRepository<DbContainer> containers,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        ILogger<ContainerRepository> logger)
    {
        this.transactionRunner = transactionRunner;
        this.containers = containers;
        this.photos = photos;
        this.itemContainerRelations = itemContainerRelations;
        this.logger = logger;
    }

    public async Task<Container?> GetAsync(string containerId)
    {
        logger.LogDebug("GetAsync: containerId={ContainerId}", containerId);

        DbContainer? dbContainer = await containers.GetAsync(containerId);
        if (dbContainer is null) return null;

        return await MapContainerWithPhotosAndRelationsAsync(dbContainer);
    }

    private Task<List<Container>> GetAllAsync()
        => GetContainersInternalAsync();

    private Task<List<Container>> GetAllAsync(int pageNumber, int pageSize)
        => GetContainersInternalAsync(pageNumber, pageSize);

    public Task<List<Container>> QueryAsync(ContainerListSpecification specification)
    {
        var (term, hasSearch) = RepositoryQueryHelpers.NormalizeSearch(specification.SearchTerm);

        if (hasSearch)
        {
            return specification.Filter == ContainerQueryFilter.Empty
                ? SearchEmptyAsync(term!)
                : SearchAsync(term!);
        }

        if (RepositoryQueryHelpers.TryGetPaging(specification.PageNumber, specification.PageSize, out var pageNumberValue, out var pageSizeValue))
        {
            return specification.Filter == ContainerQueryFilter.Empty
                ? GetEmptyAsync(pageNumberValue, pageSizeValue)
                : GetAllAsync(pageNumberValue, pageSizeValue);
        }

        return specification.Filter == ContainerQueryFilter.Empty
            ? SearchEmptyAsync(string.Empty)
            : GetAllAsync();
    }

    private async Task<List<Container>> GetEmptyAsync(int pageNumber, int pageSize)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        List<DbContainer> dbContainers = await containers.QueryAsync(
            $@"SELECT c.* FROM {nameof(DbContainer)} c
               WHERE NOT EXISTS (
                   SELECT 1 FROM {nameof(DbItemContainerRelation)} r
                   WHERE r.ContainerId = c.ContainerId)
               ORDER BY c.Name COLLATE NOCASE
               LIMIT ? OFFSET ?",
            pageSize,
            offset);

        return await MapContainersWithPhotosAndRelationsAsync(dbContainers);
    }

    private async Task<List<Container>> SearchAsync(string searchTerm)
    {
        string pattern = $"%{searchTerm}%";

        List<DbContainer> dbContainers = await containers.QueryAsync(
            $@"SELECT * FROM {nameof(DbContainer)}
               WHERE Name LIKE ? COLLATE NOCASE
                  OR Notes LIKE ? COLLATE NOCASE
               ORDER BY Name COLLATE NOCASE",
            pattern,
            pattern);

        return await MapContainersWithPhotosAndRelationsAsync(dbContainers);
    }

    private async Task<List<Container>> SearchEmptyAsync(string searchTerm)
    {
        string pattern = $"%{searchTerm}%";

        List<DbContainer> dbContainers = await containers.QueryAsync(
            $@"SELECT c.* FROM {nameof(DbContainer)} c
               WHERE (c.Name LIKE ? COLLATE NOCASE
                   OR c.Notes LIKE ? COLLATE NOCASE)
                 AND NOT EXISTS (
                   SELECT 1 FROM {nameof(DbItemContainerRelation)} r
                   WHERE r.ContainerId = c.ContainerId)
               ORDER BY c.Name COLLATE NOCASE",
            pattern,
            pattern);

        return await MapContainersWithPhotosAndRelationsAsync(dbContainers);
    }

    public async Task<int> GetItemCountInContainerAsync(string containerId)
    {
        logger.LogDebug("GetItemCountInContainerAsync: containerId={ContainerId}", containerId);

        if (!RepositoryQueryHelpers.TryParseGuid(containerId, out Guid cid, logger)) return 0;

        List<DbItemContainerRelation> relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == cid);
        // Sum quantities to match domain model behavior and guard against duplicate rows
        return relations.Sum(r => r.Quantity);
    }

    public async Task<Container?> GetContainerForItemAsync(string itemId)
    {
        logger.LogDebug("GetContainerForItemAsync: itemId={ItemId}", itemId);

        if (!RepositoryQueryHelpers.TryParseGuid(itemId, out Guid iid, logger)) return null;

        DbItemContainerRelation? relation = (await itemContainerRelations.WhereAsync(r => r.ItemId == iid)).FirstOrDefault();
        if (relation is null) return null;

        DbContainer? dbContainer = await containers.GetAsync(relation.ContainerId.ToString());
        if (dbContainer is null) return null;

        return await MapContainerWithPhotosAndRelationsAsync(dbContainer);
    }

    public async Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId)
    {
        var relations = (await itemContainerRelations.WhereAsync(
                relation => relation.ItemId == itemId && relation.Quantity > 0))
            .GroupBy(relation => relation.ContainerId)
            .Select(group => new { ContainerId = group.Key, Quantity = group.Sum(row => row.Quantity) })
            .ToList();
        var result = new List<ItemContainerAllocation>(relations.Count);

        foreach (var relation in relations)
        {
            var container = await containers.GetAsync(relation.ContainerId.ToString());
            if (container is not null)
            {
                result.Add(new ItemContainerAllocation(
                    relation.ContainerId,
                    container.Name,
                    relation.Quantity));
            }
        }

        return result.OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> GetItemContainerAllocationsAsync(
        IReadOnlyCollection<Guid> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);

        var distinctItemIds = itemIds.Where(itemId => itemId != Guid.Empty).Distinct().ToList();
        if (distinctItemIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ItemContainerAllocation>>();
        }

        var relations = (await itemContainerRelations.WhereInAsync(
                nameof(DbItemContainerRelation.ItemId),
                distinctItemIds.Select(itemId => (object)itemId).ToList()))
            .Where(relation => relation.Quantity > 0)
            .GroupBy(relation => new { relation.ItemId, relation.ContainerId })
            .Select(group => new
            {
                group.Key.ItemId,
                group.Key.ContainerId,
                Quantity = group.Sum(relation => relation.Quantity),
            })
            .ToList();
        var containerIds = relations.Select(relation => relation.ContainerId).Distinct().ToList();
        var containersById = containerIds.Count == 0
            ? new Dictionary<Guid, DbContainer>()
            : (await containers.WhereInAsync(
                    nameof(DbContainer.ContainerId),
                    containerIds.Select(containerId => (object)containerId).ToList()))
                .ToDictionary(container => container.ContainerId);

        return relations
            .Where(relation => containersById.ContainsKey(relation.ContainerId))
            .GroupBy(relation => relation.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ItemContainerAllocation>)group
                    .Select(relation => new ItemContainerAllocation(
                        relation.ContainerId,
                        containersById[relation.ContainerId].Name,
                        relation.Quantity))
                    .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }

    public async Task InsertAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        await containers.InsertAsync(container.ToDb());
    }

    public async Task UpdateAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        await containers.UpdateAsync(container.ToDb());
    }

    public async Task DeletePhotoAsync(Container container, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(container);

        await transactionRunner.RunAsync(scope =>
        {
            scope.DeleteImage(imageId, container.ContainerId);
            scope.UpdateContainer(container.ToDb());
        });
    }

    public async Task DeleteAsync(string containerId)
    {
        if (!RepositoryQueryHelpers.TryParseGuid(containerId, out Guid cid, logger)) return;

        await transactionRunner.RunAsync(scope =>
        {
            scope.DeleteImagesByOwner(cid);
            scope.DeleteRelationsByContainer(cid);
            scope.DeleteContainer(cid);
        });
    }

    #region Private Helpers - Data Loading

    private async Task<List<Container>> GetContainersInternalAsync(int? pageNumber = null, int? pageSize = null)
    {
        List<DbContainer> dbContainers = await RepositoryQueryHelpers.QueryAllOrderedByRowIdAsync(
            containers,
            pageNumber,
            pageSize);

        return await MapContainersWithPhotosAndRelationsAsync(dbContainers);
    }

    private async Task<(IEnumerable<DbImage> photos, IEnumerable<DbItemContainerRelation> relations)>
        LoadContainerPhotosAndRelationsAsync(Guid containerId)
    {
        IEnumerable<DbImage> dbPhotos = await photos.WhereAsync(p => p.OwnerUniqueId == containerId);
        IEnumerable<DbItemContainerRelation> relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == containerId);
        return (dbPhotos, relations);
    }

    #endregion

    #region Private Helpers - Mapping

    private async Task<Container> MapContainerWithPhotosAndRelationsAsync(DbContainer dbContainer)
    {
        (IEnumerable<DbImage> dbPhotos, IEnumerable<DbItemContainerRelation> relations) =
            await LoadContainerPhotosAndRelationsAsync(dbContainer.ContainerId);
        return dbContainer.ToDomain(dbPhotos, relations);
    }

    private async Task<List<Container>> MapContainersWithPhotosAndRelationsAsync(List<DbContainer> dbContainers)
    {
        if (dbContainers.Count == 0) return [];

        var sw = Stopwatch.StartNew();
        List<object> containerIds = dbContainers.Select(c => (object)c.ContainerId).ToList();
        Dictionary<Guid, IEnumerable<DbImage>> photosByContainer =
            await RepositoryQueryHelpers.LoadLookupByIdsAsync(
                photos,
                nameof(DbImage.OwnerUniqueId),
                containerIds,
                p => p.OwnerUniqueId);
        Dictionary<Guid, IEnumerable<DbItemContainerRelation>> relByContainer = await LoadRelationsByContainerIdsAsync(containerIds);
        sw.Stop();
        logger.LogInformation(
            "MapContainersWithPhotosAndRelationsAsync: containers={ContainerCount}, photosBuckets={PhotosBucketCount}, relationBuckets={RelationBucketCount}, elapsedMs={Elapsed}",
            dbContainers.Count,
            photosByContainer.Count,
            relByContainer.Count,
            sw.ElapsedMilliseconds);

        return dbContainers.Select(dbContainer =>
        {
            photosByContainer.TryGetValue(dbContainer.ContainerId, out var containerPhotos);
            relByContainer.TryGetValue(dbContainer.ContainerId, out var containerRels);
            return dbContainer.ToDomain(containerPhotos, containerRels);
        }).ToList();
    }

    private async Task<Dictionary<Guid, IEnumerable<DbItemContainerRelation>>> LoadRelationsByContainerIdsAsync(List<object> containerIds)
    {
        if (containerIds.Count == 0) return [];

        List<DbItemContainerRelation> relations = await itemContainerRelations.WhereInAsync(nameof(DbItemContainerRelation.ContainerId), containerIds);
        return RepositoryQueryHelpers.GroupByKey(relations, r => r.ContainerId);
    }

    #endregion
}
