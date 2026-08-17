
namespace CoreApp.Features.Containers.Commands;

public sealed class DeleteContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public DeleteContainerCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public Task DeleteAsync(string containerId)
        => inventoryCommands.DeleteContainerAsync(containerId);
}
