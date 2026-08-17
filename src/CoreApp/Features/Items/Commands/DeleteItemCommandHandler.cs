
namespace CoreApp.Features.Items.Commands;

public sealed class DeleteItemCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public DeleteItemCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public Task DeleteAsync(string itemId)
        => inventoryCommands.DeleteItemAsync(itemId);
}
