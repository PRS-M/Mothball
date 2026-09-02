using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Entities.Shared;
using CoreApp.Application.Features.Backup.Restore.Planning;
using CoreApp.Application.Specifications;
using CoreApp.Application.Utilities;

namespace CoreApp.Application.Features.Backup.Restore;

public sealed class InventoryBackupRestoreService : IInventoryBackupRestoreService
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IInventoryCommandRepository inventoryCommands;

    public InventoryBackupRestoreService(
        IInventoryQueryRepository inventoryQueries,
        IInventoryCommandRepository inventoryCommands)
    {
        this.inventoryQueries = inventoryQueries;
        this.inventoryCommands = inventoryCommands;
    }

    /// <inheritdoc />
    public async Task<InventoryBackupRestoreResult> RestoreFromJsonAsync(
        string backupJson,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var backup = InventoryBackupRestorePlanner.ParseBackupJson(backupJson);
        return await RestoreAsync(backup, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InventoryBackupRestoreResult> RestoreAsync(
        InventoryBackupEnvelope backup,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new InventoryBackupRestoreOptions();

        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        InventoryBackupRestorePlanner.ValidatePayloadVersion(backup);
        InventoryBackupRestorePlanner.ValidateIntegrity(backup, options);

        cancellationToken.ThrowIfCancellationRequested();

        var existingContainers = await inventoryQueries
            .QueryContainersAsync(new ContainerListSpecification(ContainerQueryFilter.All))
            .ConfigureAwait(false);

        var existingItems = await inventoryQueries
            .QueryItemsWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))
            .ConfigureAwait(false);
        var existingInventory = await inventoryQueries
            .QueryInventorySnapshotsAsync(new ItemListSpecification(ItemQueryFilter.All))
            .ConfigureAwait(false) ?? [];

        var existingState = new InventoryBackupExistingState(
            existingContainers
                .Select(c => new InventoryBackupExistingContainer(
                    c.ContainerId,
                    c.Name,
                    c.Notes,
                    c.Barcode?.Value ?? string.Empty,
                    c.Barcode is null ? null : (int)c.Barcode.Symbology))
                .ToList(),
            existingItems
                .Select(i => new InventoryBackupExistingItem(
                    i.ItemId,
                    i.Name,
                    i.Description,
                    i.Barcode?.Value ?? string.Empty,
                    i.Barcode is null ? null : (int)i.Barcode.Symbology))
                .ToList(),
            existingContainers
                .SelectMany(c => c.Photos.Select(p => new InventoryBackupImageOwnership(c.ContainerId, p.ImageId)))
                .ToList(),
            existingItems
                .SelectMany(i => i.Photos.Select(p => new InventoryBackupImageOwnership(i.ItemId, p.ImageId)))
                .ToList(),
            existingInventory
                .SelectMany(snapshot => snapshot.Allocations.Select(allocation =>
                    new InventoryBackupExistingRelation(allocation.ContainerId, snapshot.Item.ItemId, allocation.Quantity)))
                .ToList());

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existingState, options.ConflictPolicy);

        foreach (var container in plan.ContainersToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.InsertContainerAsync(CreateContainer(container))
                .ConfigureAwait(false);
        }

        foreach (var container in plan.ContainersToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.UpdateContainerAsync(CreateContainer(container))
                .ConfigureAwait(false);
        }

        foreach (var item in plan.ItemsToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.InsertItemAsync(CreateItem(item))
                .ConfigureAwait(false);
            await inventoryCommands.InsertItemInventoryAsync(new ItemInventory(item.ItemId, item.TotalQuantity))
                .ConfigureAwait(false);
        }

        foreach (var item in plan.ItemsToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.UpdateItemAsync(CreateItem(item))
                .ConfigureAwait(false);
            await inventoryCommands.SaveItemInventoryAsync(new ItemInventory(item.ItemId, item.TotalQuantity))
                .ConfigureAwait(false);
        }

        foreach (var relation in plan.RelationsToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands
                .InsertItemContainerRelation(relation.ItemId, relation.ContainerId, relation.QuantityToInsert)
                .ConfigureAwait(false);
        }

        foreach (var relation in plan.RelationsToSet)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands
                .ReplaceItemContainerRelationQuantity(relation.ItemId, relation.ContainerId, relation.Quantity)
                .ConfigureAwait(false);
        }

        foreach (var relation in plan.RelationsToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands
                .DeleteItemContainerRelation(relation.ItemId, relation.ContainerId)
                .ConfigureAwait(false);
        }

        foreach (var image in plan.ImagesToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.InsertImageItemAsync(new ImageItem(image.ImageId), image.OwnerId)
                .ConfigureAwait(false);
        }

        foreach (var image in plan.ImagesToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.DeleteImageItemAsync(image.ImageId, image.OwnerId)
                .ConfigureAwait(false);
        }

        foreach (var itemId in plan.ItemIdsToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.DeleteItemAsync(itemId.ToString())
                .ConfigureAwait(false);
        }

        foreach (var containerId in plan.ContainerIdsToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.DeleteContainerAsync(containerId.ToString())
                .ConfigureAwait(false);
        }

        return plan.Result;
    }

    private static Container CreateContainer(InventoryBackupContainer container)
    {
        var result = new Container(container.ContainerId, container.Name, container.Notes);
        result.UpdateBarcode(CreateBarcode(container.BarcodeValue, container.BarcodeSymbology));
        return result;
    }

    private static Item CreateItem(InventoryBackupItem item)
    {
        var result = new Item(item.ItemId, item.Name, item.Description);
        result.UpdateBarcode(CreateBarcode(item.BarcodeValue, item.BarcodeSymbology));
        return result;
    }

    private static Barcode? CreateBarcode(string value, int? symbology)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : new Barcode(
                value,
                symbology is int storedSymbology && Enum.IsDefined(typeof(BarcodeSymbology), storedSymbology)
                    ? (BarcodeSymbology)storedSymbology
                    : throw new ArgumentException("Backup barcode symbology is invalid.", nameof(symbology)));
}
