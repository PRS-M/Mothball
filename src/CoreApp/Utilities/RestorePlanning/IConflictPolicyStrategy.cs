using CoreApp.Contracts;

namespace CoreApp.Utilities;

internal interface IConflictPolicyStrategy
{
    void PlanRelations(PlannerContext context, IReadOnlyList<InventoryBackupRelation> validRelations);
    void PlanImages(
        PlannerContext context,
        IReadOnlyList<InventoryBackupImageRef> validContainerImages,
        IReadOnlyList<InventoryBackupImageRef> validItemImages);
}
