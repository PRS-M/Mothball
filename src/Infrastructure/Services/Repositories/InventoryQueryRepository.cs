using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using Infrastructure.Interfaces;

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

    public Task<List<Container>> GetAllContainersAsync()
        => containerRepo.GetAllAsync();

    public Task<List<Container>> GetAllContainersAsync(int pageNumber, int pageSize)
        => containerRepo.GetAllAsync(pageNumber, pageSize);

    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId)
    {
        var container = await containerRepo.GetWithItemsAndPhotosAsync(containerId);
        if (container is null) return null;

        // Avoid re-querying relations; use IDs already hydrated into the container aggregate.
        var itemIds = container.Items.Select(s => s.ItemId);
        var items = await itemRepo.GetByIdsWithPhotosAsync(itemIds);
        return (container, items);
    }

    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize)
    {
        var container = await containerRepo.GetWithItemsAndPhotosAsync(containerId, pageNumber, pageSize);
        if (container is null) return null;

        // Avoid re-querying relations; use IDs already hydrated into the container aggregate.
        var itemIds = container.Items.Select(s => s.ItemId);
        var items = await itemRepo.GetByIdsWithPhotosAsync(itemIds);
        return (container, items);
    }

    public Task<int> GetItemCountInContainerAsync(string containerId)
        => containerRepo.GetItemCountInContainerAsync(containerId);

    public Task<Container?> GetContainerForItemAsync(string itemId)
        => containerRepo.GetContainerForItemAsync(itemId);

    public Task<List<Item>> GetItemsForContainerAsync(string containerId)
        => itemRepo.GetItemsForContainerAsync(containerId);

    public Task<Item?> GetItemWithPhotosAsync(string itemId)
        => itemRepo.GetWithPhotosAsync(itemId);

    public Task<List<Item>> GetAllItemsWithPhotosAsync()
        => itemRepo.GetAllWithPhotosAsync();

    public Task<List<Item>> GetAllItemsWithPhotosAsync(int pageNumber, int pageSize)
        => itemRepo.GetAllWithPhotosAsync(pageNumber, pageSize);

    public Task<List<Item>> GetUnassignedItemsWithPhotosAsync(int pageNumber, int pageSize)
        => itemRepo.GetUnassignedWithPhotosAsync(pageNumber, pageSize);

    public Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm)
        => itemRepo.SearchWithPhotosAsync(searchTerm);

    public Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
        => itemRepo.SearchItemsInContainerAsync(containerId, searchTerm, pageNumber, pageSize);
}
