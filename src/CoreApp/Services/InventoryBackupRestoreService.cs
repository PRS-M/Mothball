using System.Text.Json;
using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Specifications;

namespace CoreApp.Services;

public sealed class InventoryBackupRestoreService : IInventoryBackupRestoreService
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IInventoryCommandRepository inventoryCommands;

    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

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
        if (string.IsNullOrWhiteSpace(backupJson))
        {
            throw new ArgumentException("Backup JSON cannot be null or empty.", nameof(backupJson));
        }

        InventoryBackupEnvelope? backup;
        try
        {
            backup = JsonSerializer.Deserialize<InventoryBackupEnvelope>(backupJson, BackupJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Backup JSON payload is invalid.", nameof(backupJson), ex);
        }

        if (backup is null)
        {
            throw new ArgumentException("Backup JSON payload is invalid.", nameof(backupJson));
        }

        return await RestoreAsync(backup, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InventoryBackupRestoreResult> RestoreAsync(
        InventoryBackupEnvelope backup,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);

        if (backup.PayloadVersion != InventoryBackupEnvelope.CurrentPayloadVersion)
        {
            throw new NotSupportedException(
                $"Unsupported backup payload version '{backup.PayloadVersion}'. Expected '{InventoryBackupEnvelope.CurrentPayloadVersion}'.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var existingContainers = await inventoryQueries
            .QueryContainersAsync(new ContainerListSpecification(ContainerQueryFilter.All))
            .ConfigureAwait(false);

        var existingItems = await inventoryQueries
            .QueryItemsWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))
            .ConfigureAwait(false);

        var knownContainerIds = existingContainers.Select(c => c.ContainerId).ToHashSet();
        var knownItemIds = existingItems.Select(i => i.ItemId).ToHashSet();

        var knownContainerImages = existingContainers
            .SelectMany(c => c.Photos.Select(p => (OwnerId: c.ContainerId, ImageId: p.ImageId)))
            .ToHashSet();

        var knownItemImages = existingItems
            .SelectMany(i => i.Photos.Select(p => (OwnerId: i.ItemId, ImageId: p.ImageId)))
            .ToHashSet();

        var knownRelationQuantityByPair = existingContainers
            .SelectMany(c => c.Items.Select(stored => (c.ContainerId, stored.ItemId, stored.Quantity)))
            .ToDictionary(
                entry => (entry.ContainerId, entry.ItemId),
                entry => entry.Quantity);

        int addedContainers = 0;
        int addedItems = 0;
        int addedRelations = 0;
        int addedRelationQuantity = 0;
        int addedImages = 0;

        int skippedExistingContainers = 0;
        int skippedExistingItems = 0;
        int skippedExistingRelations = 0;
        int skippedExistingImages = 0;
        int skippedInvalidRelations = 0;
        int skippedImagesWithMissingOwner = 0;

        foreach (var container in backup.Data.Containers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!knownContainerIds.Add(container.ContainerId))
            {
                skippedExistingContainers++;
                continue;
            }

            await inventoryCommands.InsertContainerAsync(new Container(container.ContainerId, container.Name, container.Notes))
                .ConfigureAwait(false);
            addedContainers++;
        }

        foreach (var item in backup.Data.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!knownItemIds.Add(item.ItemId))
            {
                skippedExistingItems++;
                continue;
            }

            await inventoryCommands.InsertItemAsync(new Item
            {
                ItemId = item.ItemId,
                Name = item.Name,
                Description = item.Description,
            }).ConfigureAwait(false);
            addedItems++;
        }

        foreach (var relation in backup.Data.Relations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (relation.Quantity <= 0)
            {
                skippedInvalidRelations++;
                continue;
            }

            if (!knownContainerIds.Contains(relation.ContainerId) || !knownItemIds.Contains(relation.ItemId))
            {
                skippedInvalidRelations++;
                continue;
            }

            var key = (relation.ContainerId, relation.ItemId);
            knownRelationQuantityByPair.TryGetValue(key, out int existingQuantity);
            if (relation.Quantity <= existingQuantity)
            {
                skippedExistingRelations++;
                continue;
            }

            int missingQuantity = relation.Quantity - existingQuantity;
            await inventoryCommands
                .InsertItemContainerRelation(relation.ItemId, relation.ContainerId, missingQuantity)
                .ConfigureAwait(false);

            knownRelationQuantityByPair[key] = relation.Quantity;
            addedRelations++;
            addedRelationQuantity += missingQuantity;
        }

        foreach (var image in backup.Data.Images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (image.OwnerType == InventoryBackupOwnerType.Container)
            {
                if (!knownContainerIds.Contains(image.OwnerId))
                {
                    skippedImagesWithMissingOwner++;
                    continue;
                }

                if (!knownContainerImages.Add((image.OwnerId, image.ImageId)))
                {
                    skippedExistingImages++;
                    continue;
                }

                await inventoryCommands.InsertImageItemAsync(new ImageItem(image.ImageId), image.OwnerId)
                    .ConfigureAwait(false);
                addedImages++;
                continue;
            }

            if (!knownItemIds.Contains(image.OwnerId))
            {
                skippedImagesWithMissingOwner++;
                continue;
            }

            if (!knownItemImages.Add((image.OwnerId, image.ImageId)))
            {
                skippedExistingImages++;
                continue;
            }

            await inventoryCommands.InsertImageItemAsync(new ImageItem(image.ImageId), image.OwnerId)
                .ConfigureAwait(false);
            addedImages++;
        }

        return new InventoryBackupRestoreResult
        {
            AddedContainers = addedContainers,
            AddedItems = addedItems,
            AddedRelations = addedRelations,
            AddedRelationQuantity = addedRelationQuantity,
            AddedImages = addedImages,
            SkippedExistingContainers = skippedExistingContainers,
            SkippedExistingItems = skippedExistingItems,
            SkippedExistingRelations = skippedExistingRelations,
            SkippedExistingImages = skippedExistingImages,
            SkippedInvalidRelations = skippedInvalidRelations,
            SkippedImagesWithMissingOwner = skippedImagesWithMissingOwner,
        };
    }
}
