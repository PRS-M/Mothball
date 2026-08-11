using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;

namespace CoreApp.Interfaces;

public interface IInventoryQueryRepository
{
    Task<Container?> GetContainerAsync(string containerId);

    Task<List<Container>> GetAllContainersAsync();

    Task<List<Container>> GetAllContainersAsync(int pageNumber, int pageSize);

    Task<List<Container>> GetEmptyContainersAsync(int pageNumber, int pageSize);

    Task<List<Container>> SearchContainersAsync(string searchTerm);

    Task<List<Container>> SearchEmptyContainersAsync(string searchTerm);

    Task<List<Item>> GetItemsForContainerAsync(string containerId);

    Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId);

    Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize);

    Task<int> GetItemCountInContainerAsync(string containerId);

    Task<List<Item>> GetAllItemsWithPhotosAsync();

    Task<List<Item>> GetAllItemsWithPhotosAsync(int pageNumber, int pageSize);

    Task<List<Item>> GetUnassignedItemsWithPhotosAsync(int pageNumber, int pageSize);

    Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm);

    Task<List<Item>> SearchUnassignedItemsWithPhotosAsync(string searchTerm);

    Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize);

    Task<Item?> GetItemWithPhotosAsync(string itemId);

    Task<Container?> GetContainerForItemAsync(string itemId);

    Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification);

    Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification);
}