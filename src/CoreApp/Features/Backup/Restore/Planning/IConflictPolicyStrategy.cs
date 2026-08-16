using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Restore.Planning;

internal interface IConflictPolicyStrategy
{
    void PlanRelations(PlannerContext context, IReadOnlyList<InventoryBackupRelation> validRelations);
    void PlanImages(
        PlannerContext context,
        IReadOnlyList<InventoryBackupImageRef> validContainerImages,
        IReadOnlyList<InventoryBackupImageRef> validItemImages);
}
