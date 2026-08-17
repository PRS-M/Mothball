using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.Commands;

public sealed class UpdateContainerNotesCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public UpdateContainerNotesCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public async Task UpdateAsync(Container container, string notes)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.UpdateDetails(container.Name, notes ?? string.Empty);
        await inventoryCommands.UpdateContainerAsync(container);
    }
}
