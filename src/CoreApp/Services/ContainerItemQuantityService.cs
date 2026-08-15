using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class ContainerItemQuantityService : IContainerItemQuantityService
{
    private readonly IItemInventoryCommandService inventoryCommands;

    public ContainerItemQuantityService(IItemInventoryCommandService inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public async Task<ContainerItemQuantityUpdateResult> SaveQuantityAsync(Container container, Guid itemId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(container);

        var inventoryResult = await inventoryCommands.SetContainerAllocationAsync(
            itemId,
            container.ContainerId,
            Math.Max(quantity, 0));
        container.RemoveItem(itemId);
        if (!inventoryResult.RemovedFromContainer)
        {
            container.AddItem(itemId, quantity);
        }

        return new ContainerItemQuantityUpdateResult(inventoryResult.RemovedFromContainer, container.ItemCount);
    }
}
