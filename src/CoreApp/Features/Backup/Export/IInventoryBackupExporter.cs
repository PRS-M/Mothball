using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Export;

/// <summary>
/// Defines operations for exporting inventory backups.
/// </summary>
public interface IInventoryBackupExporter
{
    /// <summary>
    /// Exports the current inventory as a backup envelope.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<InventoryBackupEnvelope> ExportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the current inventory as JSON.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the current inventory as a ZIP archive.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<byte[]> ExportAsZipAsync(CancellationToken cancellationToken = default);
}
