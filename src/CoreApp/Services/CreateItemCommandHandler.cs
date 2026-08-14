using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;

namespace CoreApp.Services;

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

    public async Task<Item> CreateAsync(string name, string description, Guid? containerId = null, int quantity = 1, byte[]? photoBytes = null)
    {
        var item = new Item(name, description);

        await inventoryCommands.InsertItemAsync(item);

        if (photoBytes is { Length: > 0 })
        {
            await imageService.SaveItemPhotoAsync(item, photoBytes);
        }

        if (containerId is { } cid && cid != Guid.Empty)
        {
            await inventoryCommands.InsertItemContainerRelation(item.ItemId, cid, quantity);
        }

        return item;
    }
}
