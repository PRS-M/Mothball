using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Restore.Planning;

internal sealed record NormalizedBackupData(
    IReadOnlyList<InventoryBackupRelation> ValidRelations,
    IReadOnlyList<InventoryBackupImageRef> ValidContainerImages,
    IReadOnlyList<InventoryBackupImageRef> ValidItemImages,
    int SkippedInvalidRelations,
    int SkippedImagesWithMissingOwner);
