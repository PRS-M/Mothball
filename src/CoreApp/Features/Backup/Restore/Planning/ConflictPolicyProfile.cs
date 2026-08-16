namespace CoreApp.Features.Backup.Restore.Planning;

internal sealed record ConflictPolicyProfile(
    bool AllowMetadataUpsert,
    bool DeleteMissingRoots,
    IConflictPolicyStrategy ChildReconciliationStrategy);
