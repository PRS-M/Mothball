using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Interfaces;

public interface IContainerAssociationQueryHandler
{
    Task<List<Container>> QueryContainersAsync(int pageNumber, int pageSize);

    Task<List<Item>> QueryUnassignedItemsAsync(int pageNumber, int pageSize);
}
