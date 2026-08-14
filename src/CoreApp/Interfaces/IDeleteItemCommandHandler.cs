namespace CoreApp.Interfaces;

public interface IDeleteItemCommandHandler
{
    Task DeleteAsync(string itemId);
}
