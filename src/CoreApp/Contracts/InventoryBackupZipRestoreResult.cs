namespace CoreApp.Contracts;

public sealed record InventoryBackupZipRestoreResult(
    InventoryBackupRestoreResult Result,
    int RestoredPhotoFiles);
