using CoreApp.Entities.InventoryAggregate;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;
using CoreApp.Contracts;

namespace CoreApp.Abstractions.Persistence;

/// <summary>
/// Defines queries for retrieving inventory data.
/// </summary>
public interface IInventoryQueryRepository
{
    /// <summary>
    /// Gets a container by its string identifier.
    /// </summary>
    /// <param name="containerId">The string identifier of the container.</param>
    Task<Container?> GetContainerAsync(string containerId);

    /// <summary>
    /// Gets the total quantity of items stored in a container.
    /// </summary>
    /// <param name="containerId">The string identifier of the container.</param>
    Task<int> GetItemCountInContainerAsync(string containerId);

    /// <summary>
    /// Gets the number of distinct items stored in a container.
    /// </summary>
    /// <param name="containerId">The string identifier of the container.</param>
    Task<int> GetDistinctItemCountInContainerAsync(string containerId);

    /// <summary>
    /// Gets an item and its associated photos by its string identifier.
    /// </summary>
    /// <param name="itemId">The string identifier of the item.</param>
    Task<Item?> GetItemWithPhotosAsync(string itemId);

    /// <summary>
    /// Gets the inventory snapshot for an item.
    /// </summary>
    /// <param name="itemId">The identifier of the item.</param>
    Task<InventorySnapshot?> GetInventorySnapshotAsync(Guid itemId);

    /// <summary>
    /// Gets the container currently associated with an item.
    /// </summary>
    /// <param name="itemId">The string identifier of the item.</param>
    Task<Container?> GetContainerForItemAsync(string itemId);

    /// <summary>
    /// Gets every container allocation for an item.
    /// </summary>
    /// <param name="itemId">The identifier of the item.</param>
    Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId);

    /// <summary>
    /// Gets container allocations grouped by item identifier.
    /// </summary>
    /// <param name="itemIds">The identifiers of the items to query.</param>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> GetItemContainerAllocationsAsync(
        IReadOnlyCollection<Guid> itemIds);

    /// <summary>
    /// Queries containers that match the supplied specification.
    /// </summary>
    /// <param name="specification">The criteria used to select containers.</param>
    Task<List<Container>> QueryContainersAsync(ContainerListSpecification specification);

    /// <summary>
    /// Queries items with their photos that match the supplied specification.
    /// </summary>
    /// <param name="specification">The criteria used to select items.</param>
    Task<List<Item>> QueryItemsWithPhotosAsync(ItemListSpecification specification);

    /// <summary>
    /// Queries inventory snapshots that match the supplied item specification.
    /// </summary>
    /// <param name="specification">The criteria used to select item inventory.</param>
    Task<List<InventorySnapshot>> QueryInventorySnapshotsAsync(ItemListSpecification specification);

    /// <summary>
    /// Queries items with photos that are assigned to a container.
    /// </summary>
    /// <param name="specification">The criteria used to select container items.</param>
    Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification);

    /// <summary>
    /// Queries inventory entries for items assigned to a container.
    /// </summary>
    /// <param name="specification">The criteria used to select container inventory entries.</param>
    Task<List<ContainerItemInventoryEntry>> QueryContainerItemInventoryAsync(
        ContainerItemsSpecification specification);
}
