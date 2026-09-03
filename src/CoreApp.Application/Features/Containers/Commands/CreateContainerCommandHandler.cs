using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.Shared;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Features.Photos;

namespace CoreApp.Application.Features.Containers.Commands;

public sealed class CreateContainerCommandHandler : ICreateContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly ImageService imageService;

    public CreateContainerCommandHandler(
        IInventoryCommandRepository inventoryCommands,
        IInventoryQueryRepository inventoryQueries,
        ImageService imageService)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    /// <inheritdoc />
    public async Task<Container> CreateAsync(string name, string notes, byte[]? photoBytes = null, Barcode? barcode = null)
    {
        await EnsureBarcodeIsAvailableAsync(barcode);

        var container = new Container(
            containerId: Guid.NewGuid(),
            name: name,
            notes: notes);
        container.UpdateBarcode(barcode);

        await inventoryCommands.InsertContainerAsync(container);

        if (photoBytes is { Length: > 0 })
        {
            await imageService.SaveContainerPhotoAsync(container, photoBytes);
        }

        return container;
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
