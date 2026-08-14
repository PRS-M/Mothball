using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class DeleteItemCommandHandler : IDeleteItemCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public DeleteItemCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public Task DeleteAsync(string itemId)
        => inventoryCommands.DeleteItemAsync(itemId);
}
