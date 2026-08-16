namespace CoreApp.Features.Containers.Commands;

public interface IAssignItemToContainerCommandHandler
{
    Task AssignAsync(Guid itemId, Guid containerId, int quantity = 1);
}
