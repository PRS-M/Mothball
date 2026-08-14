using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Interfaces;

public interface IContainerItemQuantityService
{
    Task<ContainerItemQuantityUpdateResult> SaveQuantityAsync(Container container, Guid itemId, int quantity);
}
