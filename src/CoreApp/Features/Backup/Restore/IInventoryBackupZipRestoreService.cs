using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Restore;

public interface IInventoryBackupZipRestoreService
{
    Task<InventoryBackupZipRestoreResult> RestoreFromZipAsync(
        byte[] backupZip,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default);
}
