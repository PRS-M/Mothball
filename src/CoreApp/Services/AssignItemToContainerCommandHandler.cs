using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class AssignItemToContainerCommandHandler : IAssignItemToContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public AssignItemToContainerCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public Task AssignAsync(Guid itemId, Guid containerId, int quantity = 1)
        => inventoryCommands.InsertItemContainerRelation(itemId, containerId, quantity);
}
