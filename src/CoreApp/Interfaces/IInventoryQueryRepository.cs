using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;
using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IInventoryQueryRepository
{
    Task<Container?> GetContainerAsync(string containerId);

    Task<int> GetItemCountInContainerAsync(string containerId);

    Task<Item?> GetItemWithPhotosAsync(string itemId);

    Task<ItemInventorySummary?> GetItemInventorySummaryAsync(Guid itemId);

    Task<Container?> GetContainerForItemAsync(string itemId);

    Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId);

    Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification);

    Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification);

    Task<List<ItemInventorySummary>> QueryItemInventorySummariesAsync(ItemListSpecification specification);

    Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification);

    Task<List<ContainerItemInventoryEntry>> QueryContainerItemInventoryAsync(
        ContainerItemsSpecification specification);
}
