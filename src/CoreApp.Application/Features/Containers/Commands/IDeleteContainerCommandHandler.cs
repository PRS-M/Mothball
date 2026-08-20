namespace CoreApp.Application.Features.Containers.Commands;

/// <summary>
/// Defines the command that deletes a container.
/// </summary>
public interface IDeleteContainerCommandHandler
{
    /// <summary>
    /// Deletes a container by its string identifier.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task DeleteAsync(string containerId);
}
