using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Features.Items.Commands;

/// <summary>
/// Defines the command that updates an item's description.
/// </summary>
public interface IUpdateItemDescriptionCommandHandler
{
    /// <summary>
    /// Updates an item's description.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    /// <param name="description">The value used by the operation.</param>
    Task UpdateAsync(Item item, string description);
}
