namespace CoreApp.Utilities;

internal sealed record ConflictPolicyProfile(
    bool AllowMetadataUpsert,
    bool DeleteMissingRoots,
    IConflictPolicyStrategy ChildReconciliationStrategy);
