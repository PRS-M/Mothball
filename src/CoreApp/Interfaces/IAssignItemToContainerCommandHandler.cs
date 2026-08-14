namespace CoreApp.Interfaces;

public interface IAssignItemToContainerCommandHandler
{
    Task AssignAsync(Guid itemId, Guid containerId, int quantity = 1);
}
