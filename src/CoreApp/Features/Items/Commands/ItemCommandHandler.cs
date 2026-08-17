using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Features.Items.Commands;

public sealed class ItemCommandHandler : IItemCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public ItemCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string itemId)
        => inventoryCommands.DeleteItemAsync(itemId);

    /// <inheritdoc />
    public async Task UpdateDescriptionAsync(Item item, string description)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.UpdateDetails(item.Name, description ?? string.Empty);
        await inventoryCommands.UpdateItemAsync(item);
    }
}
