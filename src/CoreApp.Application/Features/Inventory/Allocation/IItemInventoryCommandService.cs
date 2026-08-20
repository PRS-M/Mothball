using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Inventory.Allocation;

/// <summary>
/// Defines commands that update item inventory quantities and allocations.
/// </summary>
public interface IItemInventoryCommandService
{
    /// <summary>
    /// Increases an item's total quantity.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    /// <param name="totalQuantity">The value used by the operation.</param>
    Task<ItemInventoryUpdateResult> IncreaseTotalQuantityAsync(Guid itemId, int totalQuantity);

    /// <summary>
    /// Sets the quantity of an item allocated to a container.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    /// <param name="containerId">The identifier used by the operation.</param>
    /// <param name="quantity">The quantity used by the operation.</param>
    Task<ItemInventoryUpdateResult> SetContainerAllocationAsync(Guid itemId, Guid containerId, int quantity);

    /// <summary>
    /// Applies a planned inventory withdrawal to an item.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    /// <param name="plan">The value used by the operation.</param>
    Task<ItemInventoryUpdateResult> ApplyWithdrawalAsync(Guid itemId, ItemInventoryWithdrawalPlan plan);
}