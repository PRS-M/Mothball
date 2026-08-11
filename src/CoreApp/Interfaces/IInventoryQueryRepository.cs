using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;

namespace CoreApp.Interfaces;

public interface IInventoryQueryRepository
{
    Task<Container?> GetContainerAsync(string containerId);

    Task<List<Item>> GetItemsForContainerAsync(string containerId);

    Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId);

    Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize);

    Task<int> GetItemCountInContainerAsync(string containerId);

    Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize);

    Task<Item?> GetItemWithPhotosAsync(string itemId);

    Task<Container?> GetContainerForItemAsync(string itemId);

    Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification);

    Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification);

    Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification);
}