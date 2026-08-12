using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IInventoryBackupRestoreService
{
    Task<InventoryBackupRestoreResult> RestoreAsync(
        InventoryBackupEnvelope backup,
        CancellationToken cancellationToken = default);

    Task<InventoryBackupRestoreResult> RestoreFromJsonAsync(
        string backupJson,
        CancellationToken cancellationToken = default);
}
