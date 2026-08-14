using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class ContainerItemQuantityService : IContainerItemQuantityService
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public ContainerItemQuantityService(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public async Task<ContainerItemQuantityUpdateResult> SaveQuantityAsync(Container container, Guid itemId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (quantity <= 0)
        {
            await inventoryCommands.DeleteItemContainerRelation(itemId, container.ContainerId);
            container.RemoveItem(itemId);
            return new ContainerItemQuantityUpdateResult(Removed: true, container.ItemCount);
        }

        await inventoryCommands.ReplaceItemContainerRelationQuantity(itemId, container.ContainerId, quantity);
        container.RemoveItem(itemId);
        container.AddItem(itemId, quantity);

        return new ContainerItemQuantityUpdateResult(Removed: false, container.ItemCount);
    }
}
