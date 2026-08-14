using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class DeleteContainerCommandHandler : IDeleteContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public DeleteContainerCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public Task DeleteAsync(string containerId)
        => inventoryCommands.DeleteContainerAsync(containerId);
}
