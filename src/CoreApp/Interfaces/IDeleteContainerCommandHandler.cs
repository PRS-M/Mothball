namespace CoreApp.Interfaces;

public interface IDeleteContainerCommandHandler
{
    Task DeleteAsync(string containerId);
}
