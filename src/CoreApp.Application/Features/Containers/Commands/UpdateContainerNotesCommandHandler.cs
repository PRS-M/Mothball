using CoreApp.Domain.Entities.ContainerAggregate;

namespace CoreApp.Application.Features.Containers.Commands;

public sealed class UpdateContainerNotesCommandHandler : IUpdateContainerNotesCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public UpdateContainerNotesCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Container container, string notes)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.UpdateDetails(container.Name, notes ?? string.Empty);
        await inventoryCommands.UpdateContainerAsync(container);
    }
}
