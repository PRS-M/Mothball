using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Inventory.Allocation;

public interface IContainerItemQuantityService
{
    Task<ContainerItemQuantityUpdateResult> SaveQuantityAsync(Container container, Guid itemId, int quantity);
}
