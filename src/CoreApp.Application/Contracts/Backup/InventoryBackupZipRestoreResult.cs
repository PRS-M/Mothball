namespace CoreApp.Application.Contracts.Backup;

public sealed record InventoryBackupZipRestoreResult(InventoryBackupRestoreResult Result, int RestoredPhotoFiles);