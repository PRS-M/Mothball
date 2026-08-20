using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Query-side inventory repository composed from focused repositories.
/// </summary>
public class InventoryQueryRepository : IInventoryQueryRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;
    private readonly IItemInventoryRepository itemInventoryRepo;

    public InventoryQueryRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IItemInventoryRepository itemInventoryRepo)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.itemInventoryRepo = itemInventoryRepo;
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

    public Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification)
        => containerRepo.QueryAsync(specification);

    public Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification)
        => itemRepo.QueryWithPhotosAsync(specification);

    public async Task<List<InventorySnapshot>> QueryInventorySnapshotsAsync(
        ItemListSpecification specification)
    {
        var items = await itemRepo.QueryWithPhotosAsync(specification);
        var summaries = new List<InventorySnapshot>(items.Count);
        foreach (var item in items)
        {
            var inventory = await itemInventoryRepo.GetAsync(item.ItemId);
            if (inventory is not null)
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

        var entries = new List<ContainerItemInventoryEntry>(items.Count);
        foreach (var item in items)
        {
            var inventory = await itemInventoryRepo.GetAsync(item.ItemId);
            if (inventory is null)
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
}
