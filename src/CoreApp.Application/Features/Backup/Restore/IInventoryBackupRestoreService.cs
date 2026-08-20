using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Restore;

/// <summary>
/// Defines operations for restoring inventory backups.
/// </summary>
public interface IInventoryBackupRestoreService
{
    /// <summary>
    /// Restores inventory data from a backup envelope.
    /// </summary>
    /// <param name="backup">The backup envelope containing inventory data to restore.</param>
    /// <param name="options">Options that control validation and conflict handling.</param>
    /// <param name="cancellationToken">A token for cancelling the restore operation.</param>
    Task<InventoryBackupRestoreResult> RestoreAsync(
        InventoryBackupEnvelope backup,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores inventory data from a JSON backup payload.
    /// </summary>
    /// <param name="backupJson">The JSON backup payload to restore.</param>
    /// <param name="options">Options that control validation and conflict handling.</param>
    /// <param name="cancellationToken">A token for cancelling the restore operation.</param>
    Task<InventoryBackupRestoreResult> RestoreFromJsonAsync(
        string backupJson,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default);
}
