namespace CoreApp.Features.Items.Commands;

public interface IDeleteItemCommandHandler
{
    Task DeleteAsync(string itemId);
}
