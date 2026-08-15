using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IContainerAssociationQueryHandler
{
    Task<List<Container>> QueryContainersAsync(int pageNumber, int pageSize);

    Task<List<ItemInventorySummary>> QueryUnassignedItemsAsync(int pageNumber, int pageSize);
}
