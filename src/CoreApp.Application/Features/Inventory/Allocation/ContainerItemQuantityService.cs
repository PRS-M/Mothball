using CoreApp.Domain.Entities.ContainerAggregate;

namespace CoreApp.Application.Features.Inventory.Allocation;

public sealed class ContainerItemQuantityService : IContainerItemQuantityService
{
    private readonly IItemInventoryCommandService inventoryCommands;

    public ContainerItemQuantityService(IItemInventoryCommandService inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public async Task<ItemInventoryUpdateResult> SaveQuantityAsync(Container container, Guid itemId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(container);

        return await inventoryCommands.SetContainerAllocationAsync(
            itemId,
            container.ContainerId,
            Math.Max(quantity, 0));
    }
}
