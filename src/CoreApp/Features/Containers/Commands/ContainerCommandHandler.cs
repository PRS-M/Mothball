using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.Commands;

public sealed class ContainerCommandHandler : IContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public ContainerCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string containerId)
        => inventoryCommands.DeleteContainerAsync(containerId);

    /// <inheritdoc />
    public async Task UpdateNotesAsync(Container container, string notes)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.UpdateDetails(container.Name, notes ?? string.Empty);
        await inventoryCommands.UpdateContainerAsync(container);
    }
}
