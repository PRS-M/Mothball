
namespace CoreApp.Application.Features.Items.Commands;

public sealed class DeleteItemCommandHandler : IDeleteItemCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public DeleteItemCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string itemId)
        => inventoryCommands.DeleteItemAsync(itemId);
}
