using CoreApp.Contracts;
using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Interfaces;

public interface IContainerDetailsQueryHandler
{
    Task<ContainerDetailsResult?> GetDetailsAsync(string containerId);

    Task<List<Item>> QueryItemsAsync(string containerId, string? searchTerm, int pageNumber, int pageSize);
}
