using Infrastructure.Services.DatabaseModels;
using CoreApp.Application.Contracts.Backup;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Contracts;

namespace Infrastructure.Services.Restore;

public sealed class SqliteInventoryBackupRestoreService : IInventoryBackupRestoreService
{
    private readonly MothballDatabase database;
    private readonly IInventoryChangeTracker? inventoryChanges;

    public SqliteInventoryBackupRestoreService(
        MothballDatabase database,
        IInventoryChangeTracker? inventoryChanges = null)
    {
        this.database = database;
        this.inventoryChanges = inventoryChanges;
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
            var existingQuantities = connection.Table<DbItemInventory>()
                .ToDictionary(i => i.ItemId, i => i.TotalQuantity);

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
                    .Select(c => new InventoryBackupExistingContainer(
                        c.ContainerId,
                        c.Name,
                        c.Notes,
                        c.BarcodeValue,
                        c.BarcodeSymbology))
                    .ToList(),
                connection.Table<DbItem>()
                    .Select(i => new InventoryBackupExistingItem(
                        i.ItemId,
                        i.Name,
                        i.Description,
                        i.BarcodeValue,
                        i.BarcodeSymbology,
                        existingQuantities.GetValueOrDefault(i.ItemId, 1)))
                    .ToList(),
                existingContainerImages,
                existingItemImages,
                connection.Table<DbItemContainerRelation>()
                    .Select(r => new InventoryBackupExistingRelation(r.ContainerId, r.ItemId, r.Quantity))
                    .ToList());

            var plan = InventoryBackupRestorePlanner.BuildPlan(
                backup,
                existingState,
                options.ConflictPolicy,
                options.OverwriteExistingQuantities);

            foreach (var container in plan.ContainersToInsert)
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Insert(new DbContainer
                {
                    ContainerId = container.ContainerId,
                    Name = container.Name,
                    Notes = container.Notes,
                    BarcodeValue = container.BarcodeValue,
                    BarcodeSymbology = container.BarcodeSymbology,
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
                    BarcodeValue = container.BarcodeValue,
                    BarcodeSymbology = container.BarcodeSymbology,
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
                    BarcodeValue = item.BarcodeValue,
                    BarcodeSymbology = item.BarcodeSymbology,
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
                if (plan.ItemIdsWithMetadataUpdate.Contains(item.ItemId))
                {
                    connection.Update(new DbItem
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Description = item.Description,
                        BarcodeValue = item.BarcodeValue,
                        BarcodeSymbology = item.BarcodeSymbology,
                    });
                }

                if (plan.ItemIdsWithQuantityOverwrite.Contains(item.ItemId))
                {
                    connection.InsertOrReplace(new DbItemInventory
                    {
                        ItemId = item.ItemId,
                        TotalQuantity = item.TotalQuantity,
                    });
                }
            }

            foreach (var relation in plan.RelationsToInsert)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InsertOrIncreaseRelation(
                    connection,
                    relation.ItemId,
                    relation.ContainerId,
                    relation.QuantityToInsert);
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

            SyncBarcodeRegistry(connection);

            ApplyTags(connection, backup.Data, cancellationToken);

            result = plan.Result;
        }).ConfigureAwait(false);

        inventoryChanges?.MarkChanged();
        return result;
    }

    private static void SyncBarcodeRegistry(SQLite.SQLiteConnection connection)
    {
        connection.Execute($"DELETE FROM {nameof(DbBarcodeRegistry)} WHERE {nameof(DbBarcodeRegistry.Status)} = ?", (int)BarcodeRegistryStatus.Assigned);

        foreach (var container in connection.Table<DbContainer>().ToList().Where(container => !string.IsNullOrWhiteSpace(container.BarcodeValue)))
        {
            connection.Execute($"DELETE FROM {nameof(DbBarcodeRegistry)} WHERE {nameof(DbBarcodeRegistry.NormalizedValue)} = ?", container.BarcodeValue.Trim());
            connection.Insert(new DbBarcodeRegistry
            {
                Value = container.BarcodeValue.Trim(),
                NormalizedValue = container.BarcodeValue.Trim(),
                Symbology = container.BarcodeSymbology ?? (int)BarcodeSymbology.Code128,
                Status = (int)BarcodeRegistryStatus.Assigned,
                OwnerKind = (int)BarcodeOwnerKind.Container,
                OwnerId = container.ContainerId,
                OwnerName = container.Name,
            });
        }

        foreach (var item in connection.Table<DbItem>().ToList().Where(item => !string.IsNullOrWhiteSpace(item.BarcodeValue)))
        {
            connection.Execute($"DELETE FROM {nameof(DbBarcodeRegistry)} WHERE {nameof(DbBarcodeRegistry.NormalizedValue)} = ?", item.BarcodeValue.Trim());
            connection.Insert(new DbBarcodeRegistry
            {
                Value = item.BarcodeValue.Trim(),
                NormalizedValue = item.BarcodeValue.Trim(),
                Symbology = item.BarcodeSymbology ?? (int)BarcodeSymbology.Code128,
                Status = (int)BarcodeRegistryStatus.Assigned,
                OwnerKind = (int)BarcodeOwnerKind.Item,
                OwnerId = item.ItemId,
                OwnerName = item.Name,
            });
        }
    }

    private static void ApplyTags(
        SQLite.SQLiteConnection connection,
        InventoryBackupData data,
        CancellationToken cancellationToken)
    {
        var tagIdMap = new Dictionary<Guid, Guid>();
        foreach (var backupTag in data.Tags)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (backupTag.TagId == Guid.Empty || string.IsNullOrWhiteSpace(backupTag.Name))
            {
                continue;
            }

            var tagName = new TagName(backupTag.Name);
            var existing = connection.Table<DbTag>()
                .FirstOrDefault(tag => tag.NormalizedName == tagName.NormalizedValue);
            if (existing is null)
            {
                existing = new DbTag
                {
                    TagId = backupTag.TagId,
                    Name = tagName.Value,
                    NormalizedName = tagName.NormalizedValue,
                };
                connection.Insert(existing);
            }

            tagIdMap[backupTag.TagId] = existing.TagId;
        }

        var itemIds = connection.Table<DbItem>().Select(item => item.ItemId).ToHashSet();
        var containerIds = connection.Table<DbContainer>().Select(container => container.ContainerId).ToHashSet();
        connection.Execute(
            $"DELETE FROM {nameof(DbItemTag)} WHERE NOT EXISTS (SELECT 1 FROM {nameof(DbItem)} i WHERE i.ItemId = {nameof(DbItemTag)}.ItemId)");
        connection.Execute(
            $"DELETE FROM {nameof(DbContainerTag)} WHERE NOT EXISTS (SELECT 1 FROM {nameof(DbContainer)} c WHERE c.ContainerId = {nameof(DbContainerTag)}.ContainerId)");
        foreach (var assignment in data.TagAssignments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!tagIdMap.TryGetValue(assignment.TagId, out var tagId))
            {
                continue;
            }

            switch (assignment.TargetType)
            {
                case TagTargetType.Item when itemIds.Contains(assignment.TargetId):
                    connection.Execute(
                        $"INSERT OR IGNORE INTO {nameof(DbItemTag)} (ItemId, TagId) VALUES (?, ?)",
                        assignment.TargetId,
                        tagId);
                    break;
                case TagTargetType.Container when containerIds.Contains(assignment.TargetId):
                    connection.Execute(
                        $"INSERT OR IGNORE INTO {nameof(DbContainerTag)} (ContainerId, TagId) VALUES (?, ?)",
                        assignment.TargetId,
                        tagId);
                    break;
            }
        }
    }

    private static void InsertOrIncreaseRelation(
        SQLite.SQLiteConnection connection,
        Guid itemId,
        Guid containerId,
        int quantity)
    {
        var existingQuantity = connection.Table<DbItemContainerRelation>()
            .Where(relation => relation.ItemId == itemId && relation.ContainerId == containerId)
            .ToList()
            .Sum(relation => relation.Quantity);

        connection.Execute(
            $"DELETE FROM {nameof(DbItemContainerRelation)} WHERE {nameof(DbItemContainerRelation.ItemId)} = ? AND {nameof(DbItemContainerRelation.ContainerId)} = ?",
            itemId,
            containerId);

        connection.Insert(new DbItemContainerRelation
        {
            ItemId = itemId,
            ContainerId = containerId,
            Quantity = existingQuantity + quantity,
        });
    }
}
