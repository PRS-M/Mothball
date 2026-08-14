using System.Diagnostics;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Specifications;
using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Microsoft.Extensions.Logging;

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
        var (term, hasSearch) = NormalizeSearch(specification.SearchTerm);

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

        if (!TryParseGuid(containerId, out Guid cid)) return 0;

        List<DbItemContainerRelation> relations = await itemContainerRelations.WhereAsync(r => r.ContainerId == cid);
        // Sum quantities to match domain model behavior and guard against duplicate rows
        return relations.Sum(r => r.Quantity);
    }

    public async Task<Container?> GetContainerForItemAsync(string itemId)
    {
        logger.LogDebug("GetContainerForItemAsync: itemId={ItemId}", itemId);

        if (!TryParseGuid(itemId, out Guid iid)) return null;

        DbItemContainerRelation? relation = (await itemContainerRelations.WhereAsync(r => r.ItemId == iid)).FirstOrDefault();
        if (relation is null) return null;

        DbContainer? dbContainer = await containers.GetAsync(relation.ContainerId.ToString());
        if (dbContainer is null) return null;

        return await MapContainerWithPhotosAndRelationsAsync(dbContainer);
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
        if (!TryParseGuid(containerId, out Guid cid)) return;

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
        List<DbContainer> dbContainers;

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            RepositoryQueryHelpers.ValidatePaging(pageNumberValue, pageSizeValue);
            int offset = RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue);
            dbContainers = await containers.GetAllAsync(offset, pageSizeValue);
        }
        else
        {
            dbContainers = await containers.GetAllAsync();
        }

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
        Dictionary<Guid, IEnumerable<DbImage>> photosByContainer = await LoadPhotosByOwnerIdsAsync(containerIds);
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

    private async Task<Dictionary<Guid, IEnumerable<DbImage>>> LoadPhotosByOwnerIdsAsync(List<object> ownerIds)
    {
        if (ownerIds.Count == 0) return [];

        List<DbImage> photosList = await photos.WhereInAsync(nameof(DbImage.OwnerUniqueId), ownerIds);
        return RepositoryQueryHelpers.GroupByKey(photosList, p => p.OwnerUniqueId);
    }

    private async Task<Dictionary<Guid, IEnumerable<DbItemContainerRelation>>> LoadRelationsByContainerIdsAsync(List<object> containerIds)
    {
        if (containerIds.Count == 0) return [];

        List<DbItemContainerRelation> relations = await itemContainerRelations.WhereInAsync(nameof(DbItemContainerRelation.ContainerId), containerIds);
        return RepositoryQueryHelpers.GroupByKey(relations, r => r.ContainerId);
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

    private static (string? term, bool hasSearch) NormalizeSearch(string? searchTerm)
    {
        var term = searchTerm?.Trim();
        return (term, !string.IsNullOrWhiteSpace(term));
    }

    #endregion
}
