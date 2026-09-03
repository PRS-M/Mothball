using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Features.Photos;

namespace CoreApp.Application.Features.Items.Commands;

public sealed class CreateItemCommandHandler : ICreateItemCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly ImageService imageService;

    public CreateItemCommandHandler(
        IInventoryCommandRepository inventoryCommands,
        IInventoryQueryRepository inventoryQueries,
        ImageService imageService)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    /// <inheritdoc />
    public async Task<Item> CreateAsync(string name, string description, Guid? containerId = null, int quantity = 1, byte[]? photoBytes = null, Barcode? barcode = null)
    {
        await EnsureBarcodeIsAvailableAsync(barcode);

        var item = new Item(name, description);
        item.UpdateBarcode(barcode);
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

    private async Task EnsureBarcodeIsAvailableAsync(Barcode? barcode)
    {
        if (barcode is null)
        {
            return;
        }

        var existing = await inventoryQueries.FindBarcodeAsync(barcode.Value);
        if (existing is not null)
        {
            throw new BarcodeAlreadyAssignedException(barcode.Value, existing.OwnerKind, existing.OwnerName);
        }
    }
}
