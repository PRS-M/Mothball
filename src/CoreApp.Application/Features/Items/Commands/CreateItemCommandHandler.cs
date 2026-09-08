using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Photos;

namespace CoreApp.Application.Features.Items.Commands;

public sealed class CreateItemCommandHandler : ICreateItemCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly ImageService imageService;
    private readonly IBarcodeRegistryService? registry;

    public CreateItemCommandHandler(
        IInventoryCommandRepository inventoryCommands,
        IInventoryQueryRepository inventoryQueries,
        ImageService imageService,
        IBarcodeRegistryService? registry = null)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.registry = registry;
    }

    /// <inheritdoc />
    public async Task<Item> CreateAsync(string name, string description, Guid? containerId = null, int quantity = 1, byte[]? photoBytes = null, Barcode? barcode = null, bool generateInternalSku = true)
    {
        var item = new Item(name, description);
        var assignedBarcode = barcode ?? (generateInternalSku ? InternalSkuGenerator.Create(item.ItemId) : null);
        if (assignedBarcode is not null)
        {
            await EnsureBarcodeIsAvailableAsync(assignedBarcode);
            if (registry is not null)
            {
                await registry.AssignAsync(assignedBarcode, BarcodeOwnerKind.Item, item.ItemId, item.Name);
            }
        }
        item.UpdateBarcode(assignedBarcode);
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
