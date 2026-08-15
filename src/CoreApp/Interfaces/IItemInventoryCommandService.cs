using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IItemInventoryCommandService
{
    Task<ItemInventoryUpdateResult> SetTotalQuantityAsync(Guid itemId, int totalQuantity);

    Task<ItemInventoryUpdateResult> SetContainerAllocationAsync(Guid itemId, Guid containerId, int quantity);
}