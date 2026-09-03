namespace CoreApp.Application.Features.Items.Queries;

/// <summary>
/// Defines queries for item details.
/// </summary>
public interface IItemDetailsQueryHandler
{
    /// <summary>
    /// Gets the details for an item.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task<ItemDetailsResult?> GetDetailsAsync(string itemId);
}
