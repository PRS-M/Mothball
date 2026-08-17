using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.Commands;

/// <summary>
/// Defines write commands for containers.
/// </summary>
public interface IContainerCommandHandler
{
    /// <summary>
    /// Deletes a container by its string identifier.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task DeleteAsync(string containerId);

    /// <summary>
    /// Updates a container's notes.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    /// <param name="notes">The value used by the operation.</param>
    Task UpdateNotesAsync(Container container, string notes);
}
