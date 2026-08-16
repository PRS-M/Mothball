using CoreApp.Entities.Inventory;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Contracts;

namespace CoreApp.Features.Containers.Queries;

/// <summary>
/// Defines queries for container associations and unassigned items.
/// </summary>
public interface IContainerAssociationQueryHandler
{
    /// <param name="pageNumber">The value used by the operation.</param>
    /// <param name="pageSize">The value used by the operation.</param>
    Task<List<Container>> QueryContainersAsync(int pageNumber, int pageSize);

    /// <param name="searchTerm">The value used by the operation.</param>
    Task<List<Container>> QueryContainersAsync(string searchTerm);

    Task<List<InventorySnapshot>> QueryUnassignedItemsAsync(
        int pageNumber,
        int pageSize,
        Guid? excludedContainerId = null);
}
