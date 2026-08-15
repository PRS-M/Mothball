using CoreApp.Contracts;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Restore;

public sealed class SqliteInventoryBackupRestoreService : IInventoryBackupRestoreService
{
    private readonly MothballDatabase database;

    public SqliteInventoryBackupRestoreService(MothballDatabase database)
    {
        this.database = database;
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
        await database.InitializeAsync().ConfigureAwait(false);
        InventoryBackupRestoreResult result = new();

        await database.RunInTransactionAsync(connection =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingContainerIds = connection.Table<DbContainer>()
                .Select(c => c.ContainerId)
                .ToList();

            var existingItemIds = connection.Table<DbItem>()
                .Select(i => i.ItemId)
                .ToList();

            var containerIdSet = existingContainerIds.ToHashSet();
            var itemIdSet = existingItemIds.ToHashSet();

            var existingImageRows = connection.Table<DbImage>().ToList();

            var existingContainerImages = existingImageRows
                .Where(p => containerIdSet.Contains(p.OwnerUniqueId))
                .Select(p => new InventoryBackupImageOwnership(p.OwnerUniqueId, p.ImageId))
                .ToList();

            var existingItemImages = existingImageRows
                .Where(p => itemIdSet.Contains(p.OwnerUniqueId))
                .Select(p => new InventoryBackupImageOwnership(p.OwnerUniqueId, p.ImageId))
                .ToList();

            var existingState = new InventoryBackupExistingState(
                connection.Table<DbContainer>()
                    .Select(c => new InventoryBackupExistingContainer(c.ContainerId, c.Name, c.Notes))
                    .ToList(),
                connection.Table<DbItem>()
                    .Select(i => new InventoryBackupExistingItem(i.ItemId, i.Name, i.Description))
                    .ToList(),
                existingContainerImages,
                existingItemImages,
                connection.Table<DbItemContainerRelation>()
                    .Select(r => new InventoryBackupExistingRelation(r.ContainerId, r.ItemId, r.Quantity))
                    .ToList());

            var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existingState, options.ConflictPolicy);

            foreach (var container in plan.ContainersToInsert)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Insert(new DbContainer
                {
                    ContainerId = container.ContainerId,
                    Name = container.Name,
                    Notes = container.Notes,
                });
            }

            foreach (var container in plan.ContainersToUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Update(new DbContainer
                {
                    ContainerId = container.ContainerId,
                    Name = container.Name,
                    Notes = container.Notes,
                });
            }

            foreach (var item in plan.ItemsToInsert)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Insert(new DbItem
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Description = item.Description,
                });
                connection.InsertOrReplace(new DbItemInventory
                {
                    ItemId = item.ItemId,
                    TotalQuantity = item.TotalQuantity,
                });
            }

            foreach (var item in plan.ItemsToUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Update(new DbItem
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Description = item.Description,
                });
                connection.InsertOrReplace(new DbItemInventory
                {
                    ItemId = item.ItemId,
                    TotalQuantity = item.TotalQuantity,
                });
            }

            foreach (var relation in plan.RelationsToInsert)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Insert(new DbItemContainerRelation
                {
                    ItemId = relation.ItemId,
                    ContainerId = relation.ContainerId,
                    Quantity = relation.QuantityToInsert,
                });
            }

            foreach (var relation in plan.RelationsToSet)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Execute(
                    $"DELETE FROM {nameof(DbItemContainerRelation)} WHERE {nameof(DbItemContainerRelation.ItemId)} = ? AND {nameof(DbItemContainerRelation.ContainerId)} = ?",
                    relation.ItemId,
                    relation.ContainerId);

                if (relation.Quantity > 0)
                {
                    connection.Insert(new DbItemContainerRelation
                    {
                        ItemId = relation.ItemId,
                        ContainerId = relation.ContainerId,
                        Quantity = relation.Quantity,
                    });
                }
            }

            foreach (var relation in plan.RelationsToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Execute(
                    $"DELETE FROM {nameof(DbItemContainerRelation)} WHERE {nameof(DbItemContainerRelation.ItemId)} = ? AND {nameof(DbItemContainerRelation.ContainerId)} = ?",
                    relation.ItemId,
                    relation.ContainerId);
            }

            foreach (var image in plan.ImagesToInsert)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Insert(new DbImage
                {
                    ImageId = image.ImageId,
                    OwnerUniqueId = image.OwnerId,
                });
            }

            foreach (var image in plan.ImagesToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Execute(
                    $"DELETE FROM {nameof(DbImage)} WHERE {nameof(DbImage.ImageId)} = ? AND {nameof(DbImage.OwnerUniqueId)} = ?",
                    image.ImageId,
                    image.OwnerId);
            }

            foreach (var itemId in plan.ItemIdsToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Execute($"DELETE FROM {nameof(DbImage)} WHERE {nameof(DbImage.OwnerUniqueId)} = ?", itemId);
                connection.Execute($"DELETE FROM {nameof(DbItemContainerRelation)} WHERE {nameof(DbItemContainerRelation.ItemId)} = ?", itemId);
                connection.Execute($"DELETE FROM {nameof(DbItemInventory)} WHERE {nameof(DbItemInventory.ItemId)} = ?", itemId);
                connection.Execute($"DELETE FROM {nameof(DbItem)} WHERE {nameof(DbItem.ItemId)} = ?", itemId);
            }

            foreach (var containerId in plan.ContainerIdsToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Execute($"DELETE FROM {nameof(DbImage)} WHERE {nameof(DbImage.OwnerUniqueId)} = ?", containerId);
                connection.Execute($"DELETE FROM {nameof(DbItemContainerRelation)} WHERE {nameof(DbItemContainerRelation.ContainerId)} = ?", containerId);
                connection.Execute($"DELETE FROM {nameof(DbContainer)} WHERE {nameof(DbContainer.ContainerId)} = ?", containerId);
            }

            result = plan.Result;
        }).ConfigureAwait(false);

        return result;
    }
}
