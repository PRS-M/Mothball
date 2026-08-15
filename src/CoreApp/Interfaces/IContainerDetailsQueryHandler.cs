using CoreApp.Entities.Inventory;
using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IContainerDetailsQueryHandler
{
    Task<ContainerDetailsResult?> GetDetailsAsync(string containerId);

    Task<int> GetDistinctItemCountAsync(string containerId);

    Task<List<ContainerItemInventoryEntry>> QueryItemsAsync(
        string containerId,
        string? searchTerm,
        int pageNumber,
        int pageSize);
}
