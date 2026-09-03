namespace CoreApp.Application.Features.Backup.Restore.Planning;

internal sealed class PlannerContext
{
    public PlannerContext(InventoryBackupExistingState existingState)
    {
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
