using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Features.Photos;

namespace CoreApp.Application.Features.Items.Commands;

public sealed class CreateItemCommandHandler : ICreateItemCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly ImageService imageService;

    public CreateItemCommandHandler(
        IInventoryCommandRepository inventoryCommands,
        ImageService imageService)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    /// <inheritdoc />
    public async Task<Item> CreateAsync(string name, string description, Guid? containerId = null, int quantity = 1, byte[]? photoBytes = null)
    {
        var item = new Item(name, description);
        var inventory = new ItemInventory(item.ItemId, quantity);
        if (containerId is { } cid && cid != Guid.Empty)
        {
            inventory.SetContainerAllocation(cid, string.Empty, quantity);
        }

        await inventoryCommands.InsertItemAsync(item);
        await inventoryCommands.InsertItemInventoryAsync(inventory);

        if (photoBytes is { Length: > 0 })
        {
            await imageService.SaveItemPhotoAsync(item, photoBytes);
        }

        return item;
    }
}
