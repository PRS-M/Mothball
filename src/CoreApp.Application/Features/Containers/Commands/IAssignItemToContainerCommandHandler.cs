namespace CoreApp.Application.Features.Containers.Commands;

/// <summary>
/// Defines the command that assigns an item quantity to a container.
/// </summary>
public interface IAssignItemToContainerCommandHandler
{
    /// <summary>
    /// Assigns an item quantity to a container.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    /// <param name="containerId">The identifier used by the operation.</param>
    /// <param name="quantity">The quantity used by the operation.</param>
    Task AssignAsync(Guid itemId, Guid containerId, int quantity = 1);
}
