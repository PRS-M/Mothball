using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Export;

/// <summary>
/// Defines the workflow for exporting and uploading inventory backups.
/// </summary>
public interface IInventoryBackupService
{
    /// <summary>
    /// Exports the current inventory and uploads the resulting backup.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<InventoryBackupEnvelope> ExportAndUploadAsync(CancellationToken cancellationToken = default);
}
