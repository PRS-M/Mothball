using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Specifications;
using CoreApp.Utilities;

namespace CoreApp.Services;

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

    public async Task<InventoryBackupRestoreResult> RestoreFromJsonAsync(
        string backupJson,
        CancellationToken cancellationToken = default)
    {
        var backup = InventoryBackupRestorePlanner.ParseBackupJson(backupJson);
        return await RestoreAsync(backup, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InventoryBackupRestoreResult> RestoreAsync(
        InventoryBackupEnvelope backup,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        InventoryBackupRestorePlanner.ValidatePayloadVersion(backup);

        cancellationToken.ThrowIfCancellationRequested();

        var existingContainers = await inventoryQueries
            .QueryContainersAsync(new ContainerListSpecification(ContainerQueryFilter.All))
            .ConfigureAwait(false);

        var existingItems = await inventoryQueries
            .QueryItemsWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))
            .ConfigureAwait(false);

        var existingState = new InventoryBackupExistingState(
            existingContainers.Select(c => c.ContainerId).ToList(),
            existingItems.Select(i => i.ItemId).ToList(),
            existingContainers
                .SelectMany(c => c.Photos.Select(p => new InventoryBackupImageOwnership(c.ContainerId, p.ImageId)))
                .ToList(),
            existingItems
                .SelectMany(i => i.Photos.Select(p => new InventoryBackupImageOwnership(i.ItemId, p.ImageId)))
                .ToList(),
            existingContainers
                .SelectMany(c => c.Items.Select(stored => new InventoryBackupExistingRelation(c.ContainerId, stored.ItemId, stored.Quantity)))
                .ToList());

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existingState);

        foreach (var container in plan.ContainersToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.InsertContainerAsync(new Container(container.ContainerId, container.Name, container.Notes))
                .ConfigureAwait(false);
        }

        foreach (var item in plan.ItemsToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.InsertItemAsync(new Item
            {
                ItemId = item.ItemId,
                Name = item.Name,
                Description = item.Description,
            }).ConfigureAwait(false);
        }

        foreach (var relation in plan.RelationsToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands
                .InsertItemContainerRelation(relation.ItemId, relation.ContainerId, relation.QuantityToInsert)
                .ConfigureAwait(false);
        }

        foreach (var image in plan.ImagesToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.InsertImageItemAsync(new ImageItem(image.ImageId), image.OwnerId)
                .ConfigureAwait(false);
        }

        return plan.Result;
    }
}
