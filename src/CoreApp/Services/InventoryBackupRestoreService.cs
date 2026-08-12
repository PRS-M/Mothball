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
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var backup = InventoryBackupRestorePlanner.ParseBackupJson(backupJson);
        return await RestoreAsync(backup, options, cancellationToken).ConfigureAwait(false);
    }

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

        var existingState = new InventoryBackupExistingState(
            existingContainers
                .Select(c => new InventoryBackupExistingContainer(c.ContainerId, c.Name, c.Notes))
                .ToList(),
            existingItems
                .Select(i => new InventoryBackupExistingItem(i.ItemId, i.Name, i.Description))
                .ToList(),
            existingContainers
                .SelectMany(c => c.Photos.Select(p => new InventoryBackupImageOwnership(c.ContainerId, p.ImageId)))
                .ToList(),
            existingItems
                .SelectMany(i => i.Photos.Select(p => new InventoryBackupImageOwnership(i.ItemId, p.ImageId)))
                .ToList(),
            existingContainers
                .SelectMany(c => c.Items.Select(stored => new InventoryBackupExistingRelation(c.ContainerId, stored.ItemId, stored.Quantity)))
                .ToList());

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existingState, options.ConflictPolicy);

        foreach (var container in plan.ContainersToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.InsertContainerAsync(new Container(container.ContainerId, container.Name, container.Notes))
                .ConfigureAwait(false);
        }

        foreach (var container in plan.ContainersToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.UpdateContainerAsync(new Container(container.ContainerId, container.Name, container.Notes))
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

        foreach (var item in plan.ItemsToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await inventoryCommands.UpdateItemAsync(new Item
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
}
