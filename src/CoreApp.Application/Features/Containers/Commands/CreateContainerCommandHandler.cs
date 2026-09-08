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
    public async Task<Container> CreateAsync(string name, string notes, byte[]? photoBytes = null, Barcode? barcode = null, bool generateInternalSku = true, BarcodeSymbology generatedBarcodeSymbology = BarcodeSymbology.Code128)
    {
        var container = new Container(
            containerId: Guid.NewGuid(),
            name: name,
            notes: notes);
        var assignedBarcode = barcode ?? (generateInternalSku
            ? GeneratedBarcodeGenerator.Create(container.ContainerId, BarcodeOwnerKind.Container, generatedBarcodeSymbology)
            : null);
        if (assignedBarcode is not null)
        {
            await EnsureBarcodeIsAvailableAsync(assignedBarcode);
            if (registry is not null)
            {
                await registry.AssignAsync(assignedBarcode, BarcodeOwnerKind.Container, container.ContainerId, container.Name);
            }
        }
        container.UpdateBarcode(assignedBarcode);

        try
        {
            await inventoryCommands.InsertContainerAsync(container);
        }
        catch
        {
            if (registry is not null && assignedBarcode is not null)
            {
                await registry.ReleaseAsync(assignedBarcode.Value);
            }

            throw;
        }

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
