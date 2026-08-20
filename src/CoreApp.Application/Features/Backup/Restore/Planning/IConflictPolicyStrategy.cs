using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Restore.Planning;

/// <summary>
/// Defines planning behavior for a backup restore conflict policy.
/// </summary>
internal interface IConflictPolicyStrategy
{
    /// <summary>
    /// Adds relation changes to the restore plan according to the conflict policy.
    /// </summary>
    /// <param name="context">The value used by the operation.</param>
    /// <param name="validRelations">The value used by the operation.</param>
    void PlanRelations(PlannerContext context, IReadOnlyList<InventoryBackupRelation> validRelations);
    /// <summary>
    /// Adds image changes to the restore plan according to the conflict policy.
    /// </summary>
    /// <param name="context">The restore-planning context to update.</param>
    /// <param name="validContainerImages">The validated images owned by containers.</param>
    /// <param name="validItemImages">The validated images owned by items.</param>
    void PlanImages(
        PlannerContext context,
        IReadOnlyList<InventoryBackupImageRef> validContainerImages,
        IReadOnlyList<InventoryBackupImageRef> validItemImages);
}
