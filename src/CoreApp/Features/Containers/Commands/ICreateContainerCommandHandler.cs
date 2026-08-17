using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.Commands;

/// <summary>
/// Defines the command that creates a container.
/// </summary>
public interface ICreateContainerCommandHandler
{
    /// <summary>
    /// Creates a container with optional initial photo data.
    /// </summary>
    /// <param name="name">The value used by the operation.</param>
    /// <param name="notes">The value used by the operation.</param>
    /// <param name="photoBytes">The value used by the operation.</param>
    Task<Container> CreateAsync(string name, string notes, byte[]? photoBytes = null);
}
