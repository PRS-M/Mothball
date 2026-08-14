using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IInventoryBackupZipRestoreService
{
    Task<InventoryBackupZipRestoreResult> RestoreFromZipAsync(
        byte[] backupZip,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default);
}
