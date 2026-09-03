using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts;
using CoreApp.Application.Contracts.Workspace;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Query-side inventory repository composed from focused repositories.
/// </summary>
public class InventoryQueryRepository : IInventoryQueryRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;
    private readonly IItemInventoryRepository itemInventoryRepo;
    private readonly ICanonicalInventoryRepository? canonicalInventoryRepo;
    private readonly IWorkspaceContext? workspaceContext;

    public InventoryQueryRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IItemInventoryRepository itemInventoryRepo,
        ICanonicalInventoryRepository? canonicalInventoryRepo = null,
        IWorkspaceContext? workspaceContext = null)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.itemInventoryRepo = itemInventoryRepo;
        this.canonicalInventoryRepo = canonicalInventoryRepo;
        this.workspaceContext = workspaceContext;
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

        var canonicalSnapshot = await TryCreateCanonicalSnapshotAsync(item);
        if (canonicalSnapshot is not null)
        {
            return canonicalSnapshot;
        }

        var inventory = await itemInventoryRepo.GetAsync(itemId);
        return inventory is null ? null : CreateSnapshot(item, inventory);
    }

    public Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification)
        => containerRepo.QueryAsync(specification);

    public Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification)
        => itemRepo.QueryWithPhotosAsync(specification);

    public async Task<List<InventorySnapshot>> QueryInventorySnapshotsAsync(
        ItemListSpecification specification)
    {
        var items = await itemRepo.QueryWithPhotosAsync(specification);
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

    public Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification)
        => itemRepo.QueryContainerItemsWithPhotosAsync(specification);

    public async Task<List<ContainerItemInventoryEntry>> QueryContainerItemInventoryAsync(
        ContainerItemsSpecification specification)
    {
        var items = await itemRepo.QueryContainerItemsWithPhotosAsync(specification);
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

    private async Task<InventorySnapshot?> TryCreateCanonicalSnapshotAsync(Item item)
    {
        if (canonicalInventoryRepo is null || workspaceContext is null)
        {
            return null;
        }

        var defaults = (await workspaceContext.EnsureDefaultAsync()).Defaults;
        var balances = await canonicalInventoryRepo.GetBalancesAsync(new InventoryWorkspaceId(defaults.WorkspaceId), item.ItemId);
        if (balances.Count == 0)
        {
            return null;
        }

        var allocations = new List<ItemContainerAllocation>();
        foreach (var balance in balances.Where(x => x.PlacementId.Value != defaults.UnassignedLocationId && x.OnHandQuantity > 0))
        {
            var container = await containerRepo.GetAsync(balance.PlacementId.Value.ToString());
            allocations.Add(new ItemContainerAllocation(balance.PlacementId.Value, container?.Name ?? string.Empty, balance.OnHandQuantity));
        }

        var total = balances.Sum(x => x.OnHandQuantity);
        var assigned = allocations.Sum(x => x.Quantity);
        return total < 1 ? null : new InventorySnapshot(item, total, assigned, allocations);
    }
}
