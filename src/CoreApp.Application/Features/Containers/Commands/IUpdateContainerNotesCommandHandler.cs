using CoreApp.Domain.Entities.ContainerAggregate;

namespace CoreApp.Application.Features.Containers.Commands;

/// <summary>
/// Defines the command that updates container notes.
/// </summary>
public interface IUpdateContainerNotesCommandHandler
{
    /// <summary>
    /// Updates a container's notes.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    /// <param name="notes">The value used by the operation.</param>
    Task UpdateAsync(Container container, string notes);
}
