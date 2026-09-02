using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Restore.Planning;

public sealed record InventoryBackupImageOwnership(Guid OwnerId, Guid ImageId);

public sealed record InventoryBackupExistingContainer(
    Guid ContainerId,
    string Name,
    string Notes,
    string BarcodeValue = "",
    int? BarcodeSymbology = null);

public sealed record InventoryBackupExistingItem(
    Guid ItemId,
    string Name,
    string Description,
    string BarcodeValue = "",
    int? BarcodeSymbology = null);

public sealed record InventoryBackupExistingRelation(Guid ContainerId, Guid ItemId, int Quantity);

public sealed record InventoryBackupPlannedRelationInsert(Guid ContainerId, Guid ItemId, int QuantityToInsert);

public sealed record InventoryBackupPlannedRelationSet(Guid ContainerId, Guid ItemId, int Quantity);

public sealed record InventoryBackupPlannedRelationDelete(Guid ContainerId, Guid ItemId);

public sealed record InventoryBackupPlannedImageInsert(Guid OwnerId, Guid ImageId, InventoryBackupOwnerType OwnerType);

public sealed record InventoryBackupPlannedImageDelete(Guid OwnerId, Guid ImageId, InventoryBackupOwnerType OwnerType);

public sealed record InventoryBackupExistingState(
    IReadOnlyCollection<InventoryBackupExistingContainer> Containers,
    IReadOnlyCollection<InventoryBackupExistingItem> Items,
    IReadOnlyCollection<InventoryBackupImageOwnership> ContainerImages,
    IReadOnlyCollection<InventoryBackupImageOwnership> ItemImages,
    IReadOnlyCollection<InventoryBackupExistingRelation> Relations);

public sealed record InventoryBackupRestorePlan(
    List<InventoryBackupContainer> ContainersToInsert,
    List<InventoryBackupContainer> ContainersToUpdate,
    List<Guid> ContainerIdsToDelete,
    List<InventoryBackupItem> ItemsToInsert,
    List<InventoryBackupItem> ItemsToUpdate,
    List<Guid> ItemIdsToDelete,
    List<InventoryBackupPlannedRelationInsert> RelationsToInsert,
    List<InventoryBackupPlannedRelationSet> RelationsToSet,
    List<InventoryBackupPlannedRelationDelete> RelationsToDelete,
    List<InventoryBackupPlannedImageInsert> ImagesToInsert,
    List<InventoryBackupPlannedImageDelete> ImagesToDelete,
    InventoryBackupRestoreResult Result);
