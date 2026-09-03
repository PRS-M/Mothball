using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Enforces global barcode ownership before persisting barcode assignments.
/// </summary>
public sealed class BarcodeAssignmentService : IBarcodeAssignmentService
{
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IInventoryQueryRepository inventoryQueries;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarcodeAssignmentService"/> class.
    /// </summary>
    /// <param name="inventoryCommands">The repository used to persist entity updates.</param>
    /// <param name="inventoryQueries">The repository used to resolve existing barcode owners.</param>
    public BarcodeAssignmentService(
        IInventoryCommandRepository inventoryCommands,
        IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    /// <inheritdoc />
    public async Task UpdateContainerAsync(Container container, Barcode? barcode)
    {
        ArgumentNullException.ThrowIfNull(container);
        await EnsureBarcodeIsAvailableAsync(barcode, BarcodeOwnerKind.Container, container.ContainerId);
        container.UpdateBarcode(barcode);
        await inventoryCommands.UpdateContainerAsync(container);
    }

    /// <inheritdoc />
    public async Task UpdateItemAsync(Item item, Barcode? barcode)
    {
        ArgumentNullException.ThrowIfNull(item);
        await EnsureBarcodeIsAvailableAsync(barcode, BarcodeOwnerKind.Item, item.ItemId);
        item.UpdateBarcode(barcode);
        await inventoryCommands.UpdateItemAsync(item);
    }

    private async Task EnsureBarcodeIsAvailableAsync(
        Barcode? barcode,
        BarcodeOwnerKind targetOwnerKind,
        Guid targetOwnerId)
    {
        if (barcode is null)
        {
            return;
        }

        var existing = await inventoryQueries.FindBarcodeAsync(barcode.Value);
        if (existing is null || (existing.OwnerKind == targetOwnerKind && existing.OwnerId == targetOwnerId))
        {
            return;
        }

        throw new BarcodeAlreadyAssignedException(barcode.Value, existing.OwnerKind, existing.OwnerName);
    }
}