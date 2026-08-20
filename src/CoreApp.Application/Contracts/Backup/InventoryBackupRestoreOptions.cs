namespace CoreApp.Contracts.Backup;

public enum InventoryBackupConflictPolicy
{
    AddOnly,
    AddAndUpsertMetadata,
    FullSync,
    StrictFullSync,
}

public sealed record InventoryBackupRestoreOptions
{
    public InventoryBackupConflictPolicy ConflictPolicy { get; init; } = InventoryBackupConflictPolicy.AddOnly;
    public bool RequireIntegrityValidation { get; init; } = true;
    public string? SignatureSecret { get; init; }
}