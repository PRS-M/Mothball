using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Restore;

public interface IInventoryBackupRestoreService
{
    Task<InventoryBackupRestoreResult> RestoreAsync(
        InventoryBackupEnvelope backup,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<InventoryBackupRestoreResult> RestoreFromJsonAsync(
        string backupJson,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default);
}
