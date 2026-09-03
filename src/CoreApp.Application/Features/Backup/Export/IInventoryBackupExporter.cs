namespace CoreApp.Application.Features.Backup.Export;

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
    /// Exports the current inventory as a backup envelope, optionally signed with an HMAC secret.
    /// </summary>
    /// <param name="signatureSecret">The secret used to sign the payload.</param>
    /// <param name="keyId">An optional identifier for the signing key.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<InventoryBackupEnvelope> ExportAsync(
        string? signatureSecret,
        string? keyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the current inventory as JSON.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the current inventory as signed JSON.
    /// </summary>
    /// <param name="signatureSecret">The secret used to sign the payload.</param>
    /// <param name="keyId">An optional identifier for the signing key.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<string> ExportAsJsonAsync(
        string? signatureSecret,
        string? keyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the current inventory as a ZIP archive.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<byte[]> ExportAsZipAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the current inventory as a ZIP archive with signed backup metadata.
    /// </summary>
    /// <param name="signatureSecret">The secret used to sign the embedded backup metadata.</param>
    /// <param name="keyId">An optional identifier for the signing key.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<byte[]> ExportAsZipAsync(
        string? signatureSecret,
        string? keyId = null,
        CancellationToken cancellationToken = default);
}
