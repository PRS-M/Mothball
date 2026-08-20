
namespace CoreApp.Application.Features.Containers.Commands;

public sealed class DeleteContainerCommandHandler : IDeleteContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public DeleteContainerCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string containerId)
        => inventoryCommands.DeleteContainerAsync(containerId);
}
