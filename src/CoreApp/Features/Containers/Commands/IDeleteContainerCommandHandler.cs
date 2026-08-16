namespace CoreApp.Features.Containers.Commands;

public interface IDeleteContainerCommandHandler
{
    Task DeleteAsync(string containerId);
}
