using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;

namespace CoreApp.Interfaces;

public interface IInventoryQueryRepository
{
    Task<Container?> GetContainerAsync(string containerId);

    Task<int> GetItemCountInContainerAsync(string containerId);

    Task<Item?> GetItemWithPhotosAsync(string itemId);

    Task<Container?> GetContainerForItemAsync(string itemId);

    Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification);

    Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification);

    Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification);
}
