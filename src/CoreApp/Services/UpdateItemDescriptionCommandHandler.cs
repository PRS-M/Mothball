using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class UpdateItemDescriptionCommandHandler : IUpdateItemDescriptionCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;

    public UpdateItemDescriptionCommandHandler(IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
    }

    public async Task UpdateAsync(Item item, string description)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.UpdateDetails(item.Name, description ?? string.Empty);
        await inventoryCommands.UpdateItemAsync(item);
    }
}
