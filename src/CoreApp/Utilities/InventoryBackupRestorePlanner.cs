using System.Text.Json;
using CoreApp.Contracts;

namespace CoreApp.Utilities;

public sealed record InventoryBackupImageOwnership(Guid OwnerId, Guid ImageId);

public sealed record InventoryBackupExistingRelation(Guid ContainerId, Guid ItemId, int Quantity);

public sealed record InventoryBackupPlannedRelationInsert(Guid ContainerId, Guid ItemId, int QuantityToInsert);

public sealed record InventoryBackupPlannedImageInsert(Guid OwnerId, Guid ImageId, InventoryBackupOwnerType OwnerType);

public sealed record InventoryBackupExistingState(
    IReadOnlyCollection<Guid> ContainerIds,
    IReadOnlyCollection<Guid> ItemIds,
    IReadOnlyCollection<InventoryBackupImageOwnership> ContainerImages,
    IReadOnlyCollection<InventoryBackupImageOwnership> ItemImages,
    IReadOnlyCollection<InventoryBackupExistingRelation> Relations);

public sealed record InventoryBackupRestorePlan(
    List<InventoryBackupContainer> ContainersToInsert,
    List<InventoryBackupItem> ItemsToInsert,
    List<InventoryBackupPlannedRelationInsert> RelationsToInsert,
    List<InventoryBackupPlannedImageInsert> ImagesToInsert,
    InventoryBackupRestoreResult Result);

public static class InventoryBackupRestorePlanner
{
    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static InventoryBackupEnvelope ParseBackupJson(string backupJson)
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

        return backup;
    }

    public static void ValidatePayloadVersion(InventoryBackupEnvelope backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        if (backup.PayloadVersion != InventoryBackupEnvelope.CurrentPayloadVersion)
        {
            throw new NotSupportedException(
                $"Unsupported backup payload version '{backup.PayloadVersion}'. Expected '{InventoryBackupEnvelope.CurrentPayloadVersion}'.");
        }
    }

    public static InventoryBackupRestorePlan BuildPlan(
        InventoryBackupEnvelope backup,
        InventoryBackupExistingState existingState)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        ArgumentNullException.ThrowIfNull(existingState);

        var knownContainerIds = existingState.ContainerIds.ToHashSet();
        var knownItemIds = existingState.ItemIds.ToHashSet();
        var knownContainerImages = existingState.ContainerImages.ToHashSet();
        var knownItemImages = existingState.ItemImages.ToHashSet();
        var knownRelationQuantityByPair = existingState.Relations
            .GroupBy(r => (r.ContainerId, r.ItemId))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));

        var containersToInsert = new List<InventoryBackupContainer>();
        var itemsToInsert = new List<InventoryBackupItem>();
        var relationsToInsert = new List<InventoryBackupPlannedRelationInsert>();
        var imagesToInsert = new List<InventoryBackupPlannedImageInsert>();

        int skippedExistingContainers = 0;
        int skippedExistingItems = 0;
        int skippedExistingRelations = 0;
        int skippedExistingImages = 0;
        int skippedInvalidRelations = 0;
        int skippedImagesWithMissingOwner = 0;

        int addedRelationQuantity = 0;

        foreach (var container in backup.Data.Containers)
        {
            if (!knownContainerIds.Add(container.ContainerId))
            {
                skippedExistingContainers++;
                continue;
            }

            containersToInsert.Add(container);
        }

        foreach (var item in backup.Data.Items)
        {
            if (!knownItemIds.Add(item.ItemId))
            {
                skippedExistingItems++;
                continue;
            }

            itemsToInsert.Add(item);
        }

        foreach (var relation in backup.Data.Relations)
        {
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
            relationsToInsert.Add(new InventoryBackupPlannedRelationInsert(
                relation.ContainerId,
                relation.ItemId,
                missingQuantity));

            knownRelationQuantityByPair[key] = relation.Quantity;
            addedRelationQuantity += missingQuantity;
        }

        foreach (var image in backup.Data.Images)
        {
            if (image.OwnerType == InventoryBackupOwnerType.Container)
            {
                if (!knownContainerIds.Contains(image.OwnerId))
                {
                    skippedImagesWithMissingOwner++;
                    continue;
                }

                var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
                if (!knownContainerImages.Add(ownership))
                {
                    skippedExistingImages++;
                    continue;
                }

                imagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    image.OwnerId,
                    image.ImageId,
                    InventoryBackupOwnerType.Container));
                continue;
            }

            if (image.OwnerType == InventoryBackupOwnerType.Item)
            {
                if (!knownItemIds.Contains(image.OwnerId))
                {
                    skippedImagesWithMissingOwner++;
                    continue;
                }

                var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
                if (!knownItemImages.Add(ownership))
                {
                    skippedExistingImages++;
                    continue;
                }

                imagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    image.OwnerId,
                    image.ImageId,
                    InventoryBackupOwnerType.Item));
                continue;
            }

            skippedImagesWithMissingOwner++;
        }

        var result = new InventoryBackupRestoreResult
        {
            AddedContainers = containersToInsert.Count,
            AddedItems = itemsToInsert.Count,
            AddedRelations = relationsToInsert.Count,
            AddedRelationQuantity = addedRelationQuantity,
            AddedImages = imagesToInsert.Count,
            SkippedExistingContainers = skippedExistingContainers,
            SkippedExistingItems = skippedExistingItems,
            SkippedExistingRelations = skippedExistingRelations,
            SkippedExistingImages = skippedExistingImages,
            SkippedInvalidRelations = skippedInvalidRelations,
            SkippedImagesWithMissingOwner = skippedImagesWithMissingOwner,
        };

        return new InventoryBackupRestorePlan(
            containersToInsert,
            itemsToInsert,
            relationsToInsert,
            imagesToInsert,
            result);
    }
}
