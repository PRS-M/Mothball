using CoreApp.Application.Features.Inventory.Allocation;

namespace CoreApp.Application.Features.Containers.Commands;

public sealed class AssignItemToContainerCommandHandler : IAssignItemToContainerCommandHandler
{
    private readonly IItemInventoryCommandService inventoryCommands;

    public AssignItemToContainerCommandHandler(IItemInventoryCommandService inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public async Task AssignAsync(Guid itemId, Guid containerId, int quantity = 1)
        => await inventoryCommands.SetContainerAllocationAsync(itemId, containerId, quantity);
}
