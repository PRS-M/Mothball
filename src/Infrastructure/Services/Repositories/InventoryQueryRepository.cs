using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;
using Infrastructure.Interfaces;
using CoreApp.Contracts;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Query-side inventory repository composed from focused repositories.
/// </summary>
public class InventoryQueryRepository : IInventoryQueryRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;

    public InventoryQueryRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
    }

    public Task<Container?> GetContainerAsync(string containerId)
        => containerRepo.GetAsync(containerId);

    public Task<int> GetItemCountInContainerAsync(string containerId)
        => containerRepo.GetItemCountInContainerAsync(containerId);

    public Task<Container?> GetContainerForItemAsync(string itemId)
        => containerRepo.GetContainerForItemAsync(itemId);

    public Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId)
        => containerRepo.GetItemContainerAllocationsAsync(itemId);

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> GetItemContainerAllocationsAsync(
        IReadOnlyCollection<Guid> itemIds)
        => containerRepo.GetItemContainerAllocationsAsync(itemIds);

    public Task<Item?> GetItemWithPhotosAsync(string itemId)
        => itemRepo.GetWithPhotosAsync(itemId);

    public async Task<ItemInventorySummary?> GetItemInventorySummaryAsync(Guid itemId)
    {
        var item = await itemRepo.GetWithPhotosAsync(itemId.ToString());
        if (item is null)
        {
            return null;
        }

        var allocations = await containerRepo.GetItemContainerAllocationsAsync(itemId);
        return CreateSummary(item, allocations);
    }

    public Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification)
        => containerRepo.QueryAsync(specification);

    public Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification)
        => itemRepo.QueryWithPhotosAsync(specification);

    public async Task<List<ItemInventorySummary>> QueryItemInventorySummariesAsync(
        ItemListSpecification specification)
    {
        var items = await itemRepo.QueryWithPhotosAsync(specification);
        var allocationsByItem = await containerRepo.GetItemContainerAllocationsAsync(
            items.Select(item => item.ItemId).ToArray());
        var summaries = new List<ItemInventorySummary>(items.Count);
        foreach (var item in items)
        {
            var allocations = allocationsByItem.GetValueOrDefault(item.ItemId, []);
            summaries.Add(CreateSummary(item, allocations));
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

        var allocationsByItem = await containerRepo.GetItemContainerAllocationsAsync(
            items.Select(item => item.ItemId).ToArray());
        var entries = new List<ContainerItemInventoryEntry>(items.Count);
        foreach (var item in items)
        {
            var allocations = allocationsByItem.GetValueOrDefault(item.ItemId, []);
            var summary = CreateSummary(item, allocations);
            int containerQuantity = allocations
                .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.Quantity ?? 0;
            entries.Add(new ContainerItemInventoryEntry(summary, containerQuantity));
        }

        return entries;
    }

    private static ItemInventorySummary CreateSummary(
        Item item,
        IReadOnlyList<ItemContainerAllocation> allocations)
        => new(item, allocations.Sum(allocation => allocation.Quantity), allocations);
}
