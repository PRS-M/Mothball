using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Restore.Planning;

internal sealed class InventoryBackupRestorePlanBuilder
{
    public InventoryBackupRestorePlan BuildPlan(
        InventoryBackupEnvelope backup,
        InventoryBackupExistingState existingState,
        InventoryBackupConflictPolicy conflictPolicy)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        ArgumentNullException.ThrowIfNull(existingState);

        ConflictPolicyProfile policyProfile = CreatePolicyProfile(conflictPolicy);
        var context = new PlannerContext(existingState);

        PlanContainerInsertOrUpdate(backup.Data.Containers, context, policyProfile);
        PlanItemInsertOrUpdate(backup.Data.Items, context, policyProfile);
        PlanRootDeletesForSync(context, policyProfile);

        var normalized = NormalizeBackupData(backup.Data, context);
        context.SkippedInvalidRelations += normalized.SkippedInvalidRelations;
        context.SkippedImagesWithMissingOwner += normalized.SkippedImagesWithMissingOwner;

        policyProfile.ChildReconciliationStrategy.PlanRelations(context, normalized.ValidRelations);
        policyProfile.ChildReconciliationStrategy.PlanImages(context, normalized.ValidContainerImages, normalized.ValidItemImages);

        return BuildPlanResult(context);
    }

    private static ConflictPolicyProfile CreatePolicyProfile(InventoryBackupConflictPolicy conflictPolicy)
    {
        return conflictPolicy switch
        {
            InventoryBackupConflictPolicy.AddOnly => new ConflictPolicyProfile(
                AllowMetadataUpsert: false,
                DeleteMissingRoots: false,
                ChildReconciliationStrategy: AdditiveStrategy.Instance),
            InventoryBackupConflictPolicy.AddAndUpsertMetadata => new ConflictPolicyProfile(
                AllowMetadataUpsert: true,
                DeleteMissingRoots: false,
                ChildReconciliationStrategy: AdditiveStrategy.Instance),
            InventoryBackupConflictPolicy.FullSync => new ConflictPolicyProfile(
                AllowMetadataUpsert: true,
                DeleteMissingRoots: true,
                ChildReconciliationStrategy: AdditiveStrategy.Instance),
            InventoryBackupConflictPolicy.StrictFullSync => new ConflictPolicyProfile(
                AllowMetadataUpsert: true,
                DeleteMissingRoots: true,
                ChildReconciliationStrategy: StrictFullSyncStrategy.Instance),
            _ => throw new NotSupportedException($"Unsupported conflict policy '{conflictPolicy}' for restore planning."),
        };
    }

    private static void PlanContainerInsertOrUpdate(
        IReadOnlyCollection<InventoryBackupContainer> containers,
        PlannerContext context,
        ConflictPolicyProfile policyProfile)
    {
        foreach (var container in containers)
        {
            context.BackupContainerIds.Add(container.ContainerId);

            if (context.ExistingContainersById.TryGetValue(container.ContainerId, out var existing))
            {
                bool shouldUpdate = policyProfile.AllowMetadataUpsert
                    && (!string.Equals(existing.Name, container.Name, StringComparison.Ordinal)
                    || !string.Equals(existing.Notes, container.Notes, StringComparison.Ordinal));

                if (shouldUpdate)
                {
                    context.ContainersToUpdate.Add(container);
                }
                else
                {
                    context.SkippedExistingContainers++;
                }

                continue;
            }

            context.KnownContainerIds.Add(container.ContainerId);
            context.ContainersToInsert.Add(container);
        }
    }

    private static void PlanItemInsertOrUpdate(
        IReadOnlyCollection<InventoryBackupItem> items,
        PlannerContext context,
        ConflictPolicyProfile policyProfile)
    {
        foreach (var item in items)
        {
            context.BackupItemIds.Add(item.ItemId);

            if (context.ExistingItemsById.TryGetValue(item.ItemId, out var existing))
            {
                bool shouldUpdate = policyProfile.AllowMetadataUpsert
                    && (!string.Equals(existing.Name, item.Name, StringComparison.Ordinal)
                    || !string.Equals(existing.Description, item.Description, StringComparison.Ordinal));

                if (shouldUpdate)
                {
                    context.ItemsToUpdate.Add(item);
                }
                else
                {
                    context.SkippedExistingItems++;
                }

                continue;
            }

            context.KnownItemIds.Add(item.ItemId);
            context.ItemsToInsert.Add(item);
        }
    }

    private static void PlanRootDeletesForSync(PlannerContext context, ConflictPolicyProfile policyProfile)
    {
        if (!policyProfile.DeleteMissingRoots)
        {
            return;
        }

        foreach (var existingContainerId in context.ExistingContainersById.Keys)
        {
            if (!context.BackupContainerIds.Contains(existingContainerId))
            {
                context.ContainerIdsToDelete.Add(existingContainerId);
            }
        }

        foreach (var existingItemId in context.ExistingItemsById.Keys)
        {
            if (!context.BackupItemIds.Contains(existingItemId))
            {
                context.ItemIdsToDelete.Add(existingItemId);
            }
        }

        context.KnownContainerIds = context.BackupContainerIds;
        context.KnownItemIds = context.BackupItemIds;

        var relationKeysToRemove = context.KnownRelationQuantityByPair.Keys
            .Where(key => !context.KnownContainerIds.Contains(key.ContainerId) || !context.KnownItemIds.Contains(key.ItemId))
            .ToList();

        foreach (var key in relationKeysToRemove)
        {
            context.KnownRelationQuantityByPair.Remove(key);
        }

        context.KnownContainerImages = context.KnownContainerImages
            .Where(image => context.KnownContainerIds.Contains(image.OwnerId))
            .ToHashSet();

        context.KnownItemImages = context.KnownItemImages
            .Where(image => context.KnownItemIds.Contains(image.OwnerId))
            .ToHashSet();
    }

    private static NormalizedBackupData NormalizeBackupData(InventoryBackupData data, PlannerContext context)
    {
        var validRelations = new List<InventoryBackupRelation>();
        var validContainerImages = new List<InventoryBackupImageRef>();
        var validItemImages = new List<InventoryBackupImageRef>();

        int skippedInvalidRelations = 0;
        int skippedImagesWithMissingOwner = 0;

        foreach (var relation in data.Relations)
        {
            if (relation.Quantity <= 0)
            {
                skippedInvalidRelations++;
                continue;
            }

            if (!context.KnownContainerIds.Contains(relation.ContainerId) || !context.KnownItemIds.Contains(relation.ItemId))
            {
                skippedInvalidRelations++;
                continue;
            }

            validRelations.Add(relation);
        }

        foreach (var image in data.Images)
        {
            if (image.OwnerType == InventoryBackupOwnerType.Container)
            {
                if (!context.KnownContainerIds.Contains(image.OwnerId))
                {
                    skippedImagesWithMissingOwner++;
                    continue;
                }

                validContainerImages.Add(image);
                continue;
            }

            if (image.OwnerType == InventoryBackupOwnerType.Item)
            {
                if (!context.KnownItemIds.Contains(image.OwnerId))
                {
                    skippedImagesWithMissingOwner++;
                    continue;
                }

                validItemImages.Add(image);
                continue;
            }

            skippedImagesWithMissingOwner++;
        }

        return new NormalizedBackupData(
            validRelations,
            validContainerImages,
            validItemImages,
            skippedInvalidRelations,
            skippedImagesWithMissingOwner);
    }

    private static InventoryBackupRestorePlan BuildPlanResult(PlannerContext context)
    {
        var result = CreateRestoreResult(context);

        return new InventoryBackupRestorePlan(
            context.ContainersToInsert,
            context.ContainersToUpdate,
            context.ContainerIdsToDelete,
            context.ItemsToInsert,
            context.ItemsToUpdate,
            context.ItemIdsToDelete,
            context.RelationsToInsert,
            context.RelationsToSet,
            context.RelationsToDelete,
            context.ImagesToInsert,
            context.ImagesToDelete,
            result);
    }

    private static InventoryBackupRestoreResult CreateRestoreResult(PlannerContext context)
    {
        return new InventoryBackupRestoreResult
        {
            AddedContainers = context.ContainersToInsert.Count,
            AddedItems = context.ItemsToInsert.Count,
            AddedRelations = context.RelationsToInsert.Count + context.RelationsToSet.Count,
            AddedRelationQuantity = context.AddedRelationQuantity,
            AddedImages = context.ImagesToInsert.Count,
            UpdatedContainers = context.ContainersToUpdate.Count,
            UpdatedItems = context.ItemsToUpdate.Count,
            DeletedContainers = context.ContainerIdsToDelete.Count,
            DeletedItems = context.ItemIdsToDelete.Count,
            DeletedRelations = context.RelationsToDelete.Count,
            DeletedImages = context.ImagesToDelete.Count,
            SkippedExistingContainers = context.SkippedExistingContainers,
            SkippedExistingItems = context.SkippedExistingItems,
            SkippedExistingRelations = context.SkippedExistingRelations,
            SkippedExistingImages = context.SkippedExistingImages,
            SkippedInvalidRelations = context.SkippedInvalidRelations,
            SkippedImagesWithMissingOwner = context.SkippedImagesWithMissingOwner,
        };
    }
}
