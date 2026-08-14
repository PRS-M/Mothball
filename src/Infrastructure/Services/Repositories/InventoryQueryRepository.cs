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

    public Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
        => itemRepo.SearchItemsInContainerAsync(containerId, searchTerm, pageNumber, pageSize);

    public Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification)
    {
        var (term, hasSearch) = NormalizeSearch(specification.SearchTerm);

        if (hasSearch)
        {
            return specification.Filter == ContainerQueryFilter.Empty
                ? containerRepo.SearchEmptyAsync(term!)
                : containerRepo.SearchAsync(term!);
        }

        if (RepositoryQueryHelpers.TryGetPaging(specification.PageNumber, specification.PageSize, out var pageNumberValue, out var pageSizeValue))
        {
            return specification.Filter == ContainerQueryFilter.Empty
            ? containerRepo.GetEmptyAsync(pageNumberValue, pageSizeValue)
            : containerRepo.GetAllAsync(pageNumberValue, pageSizeValue);
        }

        return specification.Filter == ContainerQueryFilter.Empty
            ? containerRepo.SearchEmptyAsync(string.Empty)
            : containerRepo.GetAllAsync();
    }

    public Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification)
    {
        var (term, hasSearch) = NormalizeSearch(specification.SearchTerm);

        if (hasSearch)
        {
            return specification.Filter == ItemQueryFilter.Unassigned
                ? itemRepo.SearchUnassignedWithPhotosAsync(term!)
                : itemRepo.SearchWithPhotosAsync(term!);
        }

        if (RepositoryQueryHelpers.TryGetPaging(specification.PageNumber, specification.PageSize, out var pageNumberValue, out var pageSizeValue))
        {
            return specification.Filter == ItemQueryFilter.Unassigned
            ? itemRepo.GetUnassignedWithPhotosAsync(pageNumberValue, pageSizeValue)
            : itemRepo.GetAllWithPhotosAsync(pageNumberValue, pageSizeValue);
        }

        return specification.Filter == ItemQueryFilter.Unassigned
            ? itemRepo.SearchUnassignedWithPhotosAsync(string.Empty)
            : itemRepo.GetAllWithPhotosAsync();
    }

    public async Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification)
    {
        var (term, hasSearch) = NormalizeSearch(specification.SearchTerm);

        if (RepositoryQueryHelpers.TryGetPaging(specification.PageNumber, specification.PageSize, out var pageNumberValue, out var pageSizeValue))
        {
            if (hasSearch)
            {
                return await itemRepo.SearchItemsInContainerAsync(specification.ContainerId, term!, pageNumberValue, pageSizeValue);
            }

            var result = await GetContainerWithItemsAndPhotosAsync(specification.ContainerId, pageNumberValue, pageSizeValue);
            return result?.items ?? [];
        }

        if (hasSearch)
        {
            return await itemRepo.SearchItemsInContainerAsync(specification.ContainerId, term!, pageNumber: 0, pageSize: int.MaxValue);
        }

        return await itemRepo.GetItemsForContainerAsync(specification.ContainerId);
    }

    private static (string? term, bool hasSearch) NormalizeSearch(string? searchTerm)
    {
        var term = searchTerm?.Trim();
        return (term, !string.IsNullOrWhiteSpace(term));
    }
}
