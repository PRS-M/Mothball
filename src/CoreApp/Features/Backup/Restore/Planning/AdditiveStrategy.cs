using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Restore.Planning;

internal sealed class AdditiveStrategy : IConflictPolicyStrategy
{
    public static readonly AdditiveStrategy Instance = new();

    public void PlanRelations(PlannerContext context, IReadOnlyList<InventoryBackupRelation> validRelations)
    {
        foreach (var relation in validRelations)
        {
            var key = (relation.ContainerId, relation.ItemId);
            context.KnownRelationQuantityByPair.TryGetValue(key, out int existingQuantity);
            if (relation.Quantity <= existingQuantity)
            {
                context.SkippedExistingRelations++;
                continue;
            }

            int missingQuantity = relation.Quantity - existingQuantity;
            context.RelationsToInsert.Add(new InventoryBackupPlannedRelationInsert(
                relation.ContainerId,
                relation.ItemId,
                missingQuantity));

            context.KnownRelationQuantityByPair[key] = relation.Quantity;
            context.AddedRelationQuantity += missingQuantity;
        }
    }

    public void PlanImages(
        PlannerContext context,
        IReadOnlyList<InventoryBackupImageRef> validContainerImages,
        IReadOnlyList<InventoryBackupImageRef> validItemImages)
    {
        foreach (var image in validContainerImages)
        {
            var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
            if (!context.KnownContainerImages.Add(ownership))
            {
                context.SkippedExistingImages++;
                continue;
            }

            context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                image.OwnerId,
                image.ImageId,
                InventoryBackupOwnerType.Container));
        }

        foreach (var image in validItemImages)
        {
            var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
            if (!context.KnownItemImages.Add(ownership))
            {
                context.SkippedExistingImages++;
                continue;
            }

            context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                image.OwnerId,
                image.ImageId,
                InventoryBackupOwnerType.Item));
        }
    }
}
