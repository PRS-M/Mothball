namespace CoreApp.Features.Items.Commands;

/// <summary>
/// Defines the command that deletes an item.
/// </summary>
public interface IDeleteItemCommandHandler
{
    /// <summary>
    /// Deletes an item by its string identifier.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task DeleteAsync(string itemId);
}
