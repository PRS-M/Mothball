using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Export;

/// <summary>
/// Defines the destination for uploading an inventory backup.
/// </summary>
public interface IInventoryBackupClient
{
    /// <summary>
    /// Uploads an inventory backup to its configured destination.
    /// </summary>
    /// <param name="backup">The value used by the operation.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task UploadAsync(InventoryBackupEnvelope backup, CancellationToken cancellationToken = default);
}
