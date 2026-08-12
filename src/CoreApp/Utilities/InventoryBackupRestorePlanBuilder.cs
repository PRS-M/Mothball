using CoreApp.Contracts;

namespace CoreApp.Utilities;

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

        var context = new PlannerContext(existingState, conflictPolicy);

        PlanContainerInsertOrUpdate(backup.Data.Containers, context);
        PlanItemInsertOrUpdate(backup.Data.Items, context);
        PlanRootDeletesForSync(context);

        var normalized = NormalizeBackupData(backup.Data, context);
        context.SkippedInvalidRelations += normalized.SkippedInvalidRelations;
        context.SkippedImagesWithMissingOwner += normalized.SkippedImagesWithMissingOwner;

        IConflictPolicyStrategy strategy = CreateStrategy(conflictPolicy);
        strategy.PlanRelations(context, normalized.ValidRelations);
        strategy.PlanImages(context, normalized.ValidContainerImages, normalized.ValidItemImages);

        return BuildPlanResult(context);
    }

    private static IConflictPolicyStrategy CreateStrategy(InventoryBackupConflictPolicy conflictPolicy)
    {
        if (conflictPolicy == InventoryBackupConflictPolicy.StrictFullSync)
        {
            return StrictFullSyncStrategy.Instance;
        }

        return AdditiveStrategy.Instance;
    }

    private static void PlanContainerInsertOrUpdate(
        IReadOnlyCollection<InventoryBackupContainer> containers,
        PlannerContext context)
    {
        foreach (var container in containers)
        {
            context.BackupContainerIds.Add(container.ContainerId);

            if (context.ExistingContainersById.TryGetValue(container.ContainerId, out var existing))
            {
                bool shouldUpdate = context.ConflictPolicy != InventoryBackupConflictPolicy.AddOnly
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
        PlannerContext context)
    {
        foreach (var item in items)
        {
            context.BackupItemIds.Add(item.ItemId);

            if (context.ExistingItemsById.TryGetValue(item.ItemId, out var existing))
            {
                bool shouldUpdate = context.ConflictPolicy != InventoryBackupConflictPolicy.AddOnly
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

    private static void PlanRootDeletesForSync(PlannerContext context)
    {
        if (!context.IsFullSyncRoots)
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

    private sealed class PlannerContext
    {
        public PlannerContext(InventoryBackupExistingState existingState, InventoryBackupConflictPolicy conflictPolicy)
        {
            ConflictPolicy = conflictPolicy;
            ExistingContainersById = existingState.Containers.ToDictionary(c => c.ContainerId, c => c);
            ExistingItemsById = existingState.Items.ToDictionary(i => i.ItemId, i => i);

            KnownContainerIds = ExistingContainersById.Keys.ToHashSet();
            KnownItemIds = ExistingItemsById.Keys.ToHashSet();
            KnownContainerImages = existingState.ContainerImages.ToHashSet();
            KnownItemImages = existingState.ItemImages.ToHashSet();
            KnownRelationQuantityByPair = existingState.Relations
                .GroupBy(r => (r.ContainerId, r.ItemId))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
        }

        public InventoryBackupConflictPolicy ConflictPolicy { get; }
        public bool IsFullSyncRoots => ConflictPolicy is InventoryBackupConflictPolicy.FullSync or InventoryBackupConflictPolicy.StrictFullSync;

        public Dictionary<Guid, InventoryBackupExistingContainer> ExistingContainersById { get; }
        public Dictionary<Guid, InventoryBackupExistingItem> ExistingItemsById { get; }

        public HashSet<Guid> KnownContainerIds { get; set; }
        public HashSet<Guid> KnownItemIds { get; set; }
        public HashSet<InventoryBackupImageOwnership> KnownContainerImages { get; set; }
        public HashSet<InventoryBackupImageOwnership> KnownItemImages { get; set; }
        public Dictionary<(Guid ContainerId, Guid ItemId), int> KnownRelationQuantityByPair { get; }

        public HashSet<Guid> BackupContainerIds { get; } = [];
        public HashSet<Guid> BackupItemIds { get; } = [];

        public List<InventoryBackupContainer> ContainersToInsert { get; } = [];
        public List<InventoryBackupContainer> ContainersToUpdate { get; } = [];
        public List<Guid> ContainerIdsToDelete { get; } = [];
        public List<InventoryBackupItem> ItemsToInsert { get; } = [];
        public List<InventoryBackupItem> ItemsToUpdate { get; } = [];
        public List<Guid> ItemIdsToDelete { get; } = [];
        public List<InventoryBackupPlannedRelationInsert> RelationsToInsert { get; } = [];
        public List<InventoryBackupPlannedRelationSet> RelationsToSet { get; } = [];
        public List<InventoryBackupPlannedRelationDelete> RelationsToDelete { get; } = [];
        public List<InventoryBackupPlannedImageInsert> ImagesToInsert { get; } = [];
        public List<InventoryBackupPlannedImageDelete> ImagesToDelete { get; } = [];

        public int SkippedExistingContainers { get; set; }
        public int SkippedExistingItems { get; set; }
        public int SkippedExistingRelations { get; set; }
        public int SkippedExistingImages { get; set; }
        public int SkippedInvalidRelations { get; set; }
        public int SkippedImagesWithMissingOwner { get; set; }
        public int AddedRelationQuantity { get; set; }
    }

    private sealed record NormalizedBackupData(
        IReadOnlyList<InventoryBackupRelation> ValidRelations,
        IReadOnlyList<InventoryBackupImageRef> ValidContainerImages,
        IReadOnlyList<InventoryBackupImageRef> ValidItemImages,
        int SkippedInvalidRelations,
        int SkippedImagesWithMissingOwner);

    private interface IConflictPolicyStrategy
    {
        void PlanRelations(PlannerContext context, IReadOnlyList<InventoryBackupRelation> validRelations);
        void PlanImages(
            PlannerContext context,
            IReadOnlyList<InventoryBackupImageRef> validContainerImages,
            IReadOnlyList<InventoryBackupImageRef> validItemImages);
    }

    private sealed class AdditiveStrategy : IConflictPolicyStrategy
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

    private sealed class StrictFullSyncStrategy : IConflictPolicyStrategy
    {
        public static readonly StrictFullSyncStrategy Instance = new();

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
}
