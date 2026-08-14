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

    public Task<int> GetItemCountInContainerAsync(string containerId)
        => containerRepo.GetItemCountInContainerAsync(containerId);

    public Task<Container?> GetContainerForItemAsync(string itemId)
        => containerRepo.GetContainerForItemAsync(itemId);

    public Task<Item?> GetItemWithPhotosAsync(string itemId)
        => itemRepo.GetWithPhotosAsync(itemId);

    public Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification)
        => containerRepo.QueryAsync(specification);

    public Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification)
        => itemRepo.QueryWithPhotosAsync(specification);

    public Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification)
        => itemRepo.QueryContainerItemsWithPhotosAsync(specification);
}
