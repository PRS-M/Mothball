using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Restore.Planning;

internal sealed class StrictFullSyncStrategy : IConflictPolicyStrategy
{
    public static readonly StrictFullSyncStrategy Instance = new();

    /// <inheritdoc />
    public void PlanRelations(PlannerContext context, IReadOnlyList<InventoryBackupRelation> validRelations)
    {
        var desiredRelationQuantityByPair = new Dictionary<(Guid ContainerId, Guid ItemId), int>();

        foreach (var relation in validRelations)
        {
            var key = (relation.ContainerId, relation.ItemId);
            desiredRelationQuantityByPair.TryGetValue(key, out int current);
            desiredRelationQuantityByPair[key] = current + relation.Quantity;
        }

        foreach (var (key, desiredQuantity) in desiredRelationQuantityByPair)
        {
            context.KnownRelationQuantityByPair.TryGetValue(key, out int existingQuantity);
            if (existingQuantity == desiredQuantity)
            {
                context.SkippedExistingRelations++;
                continue;
            }

            context.RelationsToSet.Add(new InventoryBackupPlannedRelationSet(key.ContainerId, key.ItemId, desiredQuantity));
            if (desiredQuantity > existingQuantity)
            {
                context.AddedRelationQuantity += desiredQuantity - existingQuantity;
            }
        }

        foreach (var key in context.KnownRelationQuantityByPair.Keys)
        {
            if (!desiredRelationQuantityByPair.ContainsKey(key))
            {
                context.RelationsToDelete.Add(new InventoryBackupPlannedRelationDelete(key.ContainerId, key.ItemId));
            }
        }
    }

    /// <inheritdoc />
    public void PlanImages(
        PlannerContext context,
        IReadOnlyList<InventoryBackupImageRef> validContainerImages,
        IReadOnlyList<InventoryBackupImageRef> validItemImages)
    {
        var desiredContainerImages = validContainerImages
            .Select(image => new InventoryBackupImageOwnership(image.OwnerId, image.ImageId))
            .ToHashSet();

        var desiredItemImages = validItemImages
            .Select(image => new InventoryBackupImageOwnership(image.OwnerId, image.ImageId))
            .ToHashSet();

        foreach (var ownership in desiredContainerImages)
        {
            if (!context.KnownContainerImages.Contains(ownership))
            {
                context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    ownership.OwnerId,
                    ownership.ImageId,
                    InventoryBackupOwnerType.Container));
            }
            else
            {
                context.SkippedExistingImages++;
            }
        }

        foreach (var ownership in desiredItemImages)
        {
            if (!context.KnownItemImages.Contains(ownership))
            {
                context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    ownership.OwnerId,
                    ownership.ImageId,
                    InventoryBackupOwnerType.Item));
            }
            else
            {
                context.SkippedExistingImages++;
            }
        }

        foreach (var existingImage in context.KnownContainerImages)
        {
            if (!desiredContainerImages.Contains(existingImage))
            {
                context.ImagesToDelete.Add(new InventoryBackupPlannedImageDelete(
                    existingImage.OwnerId,
                    existingImage.ImageId,
                    InventoryBackupOwnerType.Container));
            }
        }

        foreach (var existingImage in context.KnownItemImages)
        {
            if (!desiredItemImages.Contains(existingImage))
            {
                context.ImagesToDelete.Add(new InventoryBackupPlannedImageDelete(
                    existingImage.OwnerId,
                    existingImage.ImageId,
                    InventoryBackupOwnerType.Item));
            }
        }
    }
}
