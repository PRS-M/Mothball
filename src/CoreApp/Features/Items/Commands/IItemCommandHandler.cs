using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Features.Items.Commands;

/// <summary>
/// Defines write commands for items.
/// </summary>
public interface IItemCommandHandler
{
    /// <summary>
    /// Deletes an item by its string identifier.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task DeleteAsync(string itemId);

    /// <summary>
    /// Updates an item's description.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    /// <param name="description">The value used by the operation.</param>
    Task UpdateDescriptionAsync(Item item, string description);
}
