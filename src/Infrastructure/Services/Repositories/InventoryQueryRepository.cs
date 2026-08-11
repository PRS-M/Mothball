using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;
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

    public Task<List<Container>> GetEmptyContainersAsync(int pageNumber, int pageSize)
        => containerRepo.GetEmptyAsync(pageNumber, pageSize);

    public Task<List<Container>> SearchContainersAsync(string searchTerm)
        => containerRepo.SearchAsync(searchTerm);

    public Task<List<Container>> SearchEmptyContainersAsync(string searchTerm)
        => containerRepo.SearchEmptyAsync(searchTerm);

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

    public Task<List<Item>> SearchUnassignedItemsWithPhotosAsync(string searchTerm)
        => itemRepo.SearchUnassignedWithPhotosAsync(searchTerm);

    public Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
        => itemRepo.SearchItemsInContainerAsync(containerId, searchTerm, pageNumber, pageSize);

    public Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification)
    {
        var term = specification.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(term);

        if (hasSearch)
        {
            return specification.Filter == ContainerQueryFilter.Empty
                ? containerRepo.SearchEmptyAsync(term!)
                : containerRepo.SearchAsync(term!);
        }

        if (specification.PageNumber.HasValue && specification.PageSize.HasValue)
        {
            return specification.Filter == ContainerQueryFilter.Empty
                ? containerRepo.GetEmptyAsync(specification.PageNumber.Value, specification.PageSize.Value)
                : containerRepo.GetAllAsync(specification.PageNumber.Value, specification.PageSize.Value);
        }

        return specification.Filter == ContainerQueryFilter.Empty
            ? containerRepo.SearchEmptyAsync(string.Empty)
            : containerRepo.GetAllAsync();
    }

    public Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification)
    {
        var term = specification.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(term);

        if (hasSearch)
        {
            return specification.Filter == ItemQueryFilter.Unassigned
                ? itemRepo.SearchUnassignedWithPhotosAsync(term!)
                : itemRepo.SearchWithPhotosAsync(term!);
        }

        if (specification.PageNumber.HasValue && specification.PageSize.HasValue)
        {
            return specification.Filter == ItemQueryFilter.Unassigned
                ? itemRepo.GetUnassignedWithPhotosAsync(specification.PageNumber.Value, specification.PageSize.Value)
                : itemRepo.GetAllWithPhotosAsync(specification.PageNumber.Value, specification.PageSize.Value);
        }

        return specification.Filter == ItemQueryFilter.Unassigned
            ? itemRepo.SearchUnassignedWithPhotosAsync(string.Empty)
            : itemRepo.GetAllWithPhotosAsync();
    }

    public async Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification)
    {
        var term = specification.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(term);

        if (specification.PageNumber.HasValue && specification.PageSize.HasValue)
        {
            var pageNumber = specification.PageNumber.Value;
            var pageSize = specification.PageSize.Value;

            if (hasSearch)
            {
                return await itemRepo.SearchItemsInContainerAsync(specification.ContainerId, term!, pageNumber, pageSize);
            }

            var result = await GetContainerWithItemsAndPhotosAsync(specification.ContainerId, pageNumber, pageSize);
            return result?.items ?? [];
        }

        if (hasSearch)
        {
            return await itemRepo.SearchItemsInContainerAsync(specification.ContainerId, term!, pageNumber: 0, pageSize: int.MaxValue);
        }

        return await itemRepo.GetItemsForContainerAsync(specification.ContainerId);
    }
}
