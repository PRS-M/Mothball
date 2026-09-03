using CoreApp.Domain.Entities.ContainerAggregate;

namespace CoreApp.Application.Features.Inventory.Allocation;

/// <summary>
/// Defines operations for setting an item's quantity in a container.
/// </summary>
public interface IContainerItemQuantityService
{
    /// <summary>
    /// Saves an item's quantity in a container.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    /// <param name="itemId">The identifier used by the operation.</param>
    /// <param name="quantity">The quantity used by the operation.</param>
    Task<ItemInventoryUpdateResult> SaveQuantityAsync(Container container, Guid itemId, int quantity);
}
