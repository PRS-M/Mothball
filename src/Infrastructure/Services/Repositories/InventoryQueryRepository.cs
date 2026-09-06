using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts;
using CoreApp.Application.Contracts.Tags;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Query-side inventory repository composed from focused repositories.
/// </summary>
public class InventoryQueryRepository : IInventoryQueryRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;
    private readonly IItemInventoryRepository itemInventoryRepo;
    private readonly ITagRepository? tagRepository;

    public InventoryQueryRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IItemInventoryRepository itemInventoryRepo,
        ITagRepository? tagRepository = null)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.itemInventoryRepo = itemInventoryRepo;
        this.tagRepository = tagRepository;
    }

    /// <inheritdoc />
    public async Task<BarcodeLookupResult?> FindBarcodeAsync(string barcodeValue)
    {
        var normalizedValue = barcodeValue?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        var container = await containerRepo.FindByBarcodeAsync(normalizedValue);
        if (container is not null)
        {
            return new BarcodeLookupResult(BarcodeOwnerKind.Container, container.ContainerId, container.Name);
        }

        var item = await itemRepo.FindByBarcodeAsync(normalizedValue);
        return item is null
            ? null
            : new BarcodeLookupResult(BarcodeOwnerKind.Item, item.ItemId, item.Name);
    }

    /// <inheritdoc />
    public Task<Container?> GetContainerAsync(string containerId)
        => containerRepo.GetAsync(containerId);

    /// <inheritdoc />
    public Task<int> GetItemCountInContainerAsync(string containerId)
        => containerRepo.GetItemCountInContainerAsync(containerId);

    /// <inheritdoc />
    public Task<int> GetDistinctItemCountInContainerAsync(string containerId)
        => containerRepo.GetDistinctItemCountInContainerAsync(containerId);

    /// <inheritdoc />
    public Task<Container?> GetContainerForItemAsync(string itemId)
        => containerRepo.GetContainerForItemAsync(itemId);

    public Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId)
        => containerRepo.GetItemContainerAllocationsAsync(itemId);

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> GetItemContainerAllocationsAsync(
        IReadOnlyCollection<Guid> itemIds)
        => containerRepo.GetItemContainerAllocationsAsync(itemIds);

    /// <inheritdoc />
    public Task<Item?> GetItemWithPhotosAsync(string itemId)
        => itemRepo.GetWithPhotosAsync(itemId);

    /// <inheritdoc />
    public async Task<InventorySnapshot?> GetInventorySnapshotAsync(Guid itemId)
    {
        var item = await itemRepo.GetWithPhotosAsync(itemId.ToString());
        if (item is null)
        {
            return null;
        }

        var inventory = await itemInventoryRepo.GetAsync(itemId);
        return inventory is null ? null : CreateSnapshot(item, inventory);
    }

    public async Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification)
    {
        if (specification.TagCriteria is null)
        {
            return await containerRepo.QueryAsync(specification).ConfigureAwait(false);
        }

        var unpaged = await containerRepo.QueryAsync(specification with { PageNumber = null, PageSize = null })
            .ConfigureAwait(false);
        var filtered = await FilterByTagsAsync(unpaged, TagTargetType.Container, specification.TagCriteria)
            .ConfigureAwait(false);
        return ApplyPaging(filtered, specification.PageNumber, specification.PageSize);
    }

    public async Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification)
    {
        if (specification.TagCriteria is null)
        {
            return await itemRepo.QueryWithPhotosAsync(specification).ConfigureAwait(false);
        }

        var unpaged = await itemRepo.QueryWithPhotosAsync(specification with { PageNumber = null, PageSize = null })
            .ConfigureAwait(false);
        var filtered = await FilterByTagsAsync(unpaged, TagTargetType.Item, specification.TagCriteria)
            .ConfigureAwait(false);
        return ApplyPaging(filtered, specification.PageNumber, specification.PageSize);
    }

    public async Task<List<InventorySnapshot>> QueryInventorySnapshotsAsync(
        ItemListSpecification specification)
    {
        var items = await QueryItemsWithPhotosAsync(specification).ConfigureAwait(false);
        var inventories = await itemInventoryRepo.GetManyAsync(items.Select(item => item.ItemId).ToList());
        var summaries = new List<InventorySnapshot>(items.Count);
        foreach (var item in items)
        {
            if (inventories.TryGetValue(item.ItemId, out var inventory))
            {
                summaries.Add(CreateSnapshot(item, inventory));
            }
        }

        return summaries;
    }

    public async Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification)
    {
        if (specification.TagCriteria is null)
        {
            return await itemRepo.QueryContainerItemsWithPhotosAsync(specification).ConfigureAwait(false);
        }

        var unpaged = await itemRepo.QueryContainerItemsWithPhotosAsync(specification with { PageNumber = null, PageSize = null })
            .ConfigureAwait(false);
        var filtered = await FilterByTagsAsync(unpaged, TagTargetType.Item, specification.TagCriteria)
            .ConfigureAwait(false);
        return ApplyPaging(filtered, specification.PageNumber, specification.PageSize);
    }

    public async Task<List<ContainerItemInventoryEntry>> QueryContainerItemInventoryAsync(
        ContainerItemsSpecification specification)
    {
        var items = await QueryContainerItemsWithPhotosAsync(specification).ConfigureAwait(false);
        if (!Guid.TryParse(specification.ContainerId, out var containerId))
        {
            return [];
        }

        var inventories = await itemInventoryRepo.GetManyAsync(items.Select(item => item.ItemId).ToList());
        var entries = new List<ContainerItemInventoryEntry>(items.Count);
        foreach (var item in items)
        {
            if (!inventories.TryGetValue(item.ItemId, out var inventory))
            {
                continue;
            }

            var summary = CreateSnapshot(item, inventory);
            int containerQuantity = inventory.Allocations
                .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.Quantity ?? 0;
            entries.Add(new ContainerItemInventoryEntry(summary, containerQuantity));
        }

        return entries;
    }

    private static InventorySnapshot CreateSnapshot(Item item, ItemInventory inventory)
        => new(item, inventory.TotalQuantity, inventory.AssignedQuantity, inventory.Allocations);

    private async Task<List<T>> FilterByTagsAsync<T>(
        IReadOnlyCollection<T> entities,
        TagTargetType targetType,
        TagFilter criteria)
        where T : class
    {
        if (tagRepository is null || criteria.TargetType != targetType)
        {
            return criteria.TargetType == targetType ? [] : [];
        }

        var normalizedNames = criteria.NormalizedNames;
        if (normalizedNames.Count == 0)
        {
            return [];
        }

        var result = new List<T>();
        foreach (var entity in entities)
        {
            Guid targetId = entity switch
            {
                Container container => container.ContainerId,
                Item item => item.ItemId,
                _ => throw new NotSupportedException($"Unsupported tag query entity '{typeof(T).Name}'."),
            };

            var names = (await tagRepository.GetForTargetAsync(targetType, targetId).ConfigureAwait(false))
                .Select(tag => tag.Name.NormalizedValue)
                .ToHashSet(StringComparer.Ordinal);
            bool matches = criteria.MatchAll
                ? normalizedNames.All(names.Contains)
                : normalizedNames.Any(names.Contains);
            if (matches)
            {
                result.Add(entity);
            }
        }

        return result;
    }

    private static List<T> ApplyPaging<T>(IReadOnlyList<T> values, int? pageNumber, int? pageSize)
    {
        if (pageNumber is null && pageSize is null)
        {
            return values.ToList();
        }

        if (pageNumber is not int page || pageSize is not int size || page < 0 || size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be non-negative and page size must be positive.");
        }

        return values.Skip(checked(page * size)).Take(size).ToList();
    }
}
