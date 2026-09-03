namespace CoreApp.Application.Features.Inventory.Allocation;

/// <summary>
/// Receives stock for an existing item into unassigned inventory or a selected container.
/// </summary>
public interface IItemReceiptService
{
    /// <summary>
    /// Adds the received quantity to an item and optionally assigns the received quantity to a container.
    /// </summary>
    /// <param name="itemId">The identifier of the existing item receiving stock.</param>
    /// <param name="quantity">The positive quantity to receive.</param>
    /// <param name="containerId">The optional destination container; <see langword="null"/> retains the stock as unassigned.</param>
    /// <returns>The resulting inventory quantity summary.</returns>
    Task<ItemInventoryUpdateResult> ReceiveAsync(Guid itemId, int quantity, Guid? containerId = null);
}