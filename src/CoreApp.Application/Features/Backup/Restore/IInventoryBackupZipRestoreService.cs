namespace CoreApp.Application.Features.Backup.Restore;

/// <summary>
/// Defines operations for restoring compressed inventory backups.
/// </summary>
public interface IInventoryBackupZipRestoreService
{
    /// <summary>
    /// Restores inventory data from a ZIP backup archive.
    /// </summary>
    /// <param name="backupZip">The ZIP backup archive to restore.</param>
    /// <param name="options">Options that control validation and conflict handling.</param>
    /// <param name="cancellationToken">A token for cancelling the restore operation.</param>
    Task<InventoryBackupZipRestoreResult> RestoreFromZipAsync(
        byte[] backupZip,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default);
}
