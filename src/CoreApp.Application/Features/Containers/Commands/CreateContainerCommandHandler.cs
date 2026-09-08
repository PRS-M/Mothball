using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Photos;

namespace CoreApp.Application.Features.Containers.Commands;

public sealed class CreateContainerCommandHandler : ICreateContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly ImageService imageService;
    private readonly IBarcodeRegistryService? registry;

    public CreateContainerCommandHandler(
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
    public async Task<Container> CreateAsync(string name, string notes, byte[]? photoBytes = null, Barcode? barcode = null)
    {
        var container = new Container(
            containerId: Guid.NewGuid(),
            name: name,
            notes: notes);
        var assignedBarcode = barcode ?? InternalSkuGenerator.Create(container.ContainerId);
        await EnsureBarcodeIsAvailableAsync(assignedBarcode);
        if (registry is not null)
        {
            await registry.AssignAsync(assignedBarcode, BarcodeOwnerKind.Container, container.ContainerId, container.Name);
        }
        container.UpdateBarcode(assignedBarcode);

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
