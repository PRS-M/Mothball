namespace CoreApp.Contracts;

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

    // When true, checksum validation is mandatory and restore fails if integrity metadata is missing.
    public bool RequireIntegrityValidation { get; init; } = true;

    // Optional secret used when a payload carries HMAC signature metadata.
    public string? SignatureSecret { get; init; }
}
