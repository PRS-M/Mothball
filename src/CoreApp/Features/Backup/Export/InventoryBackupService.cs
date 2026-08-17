using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Export;

public sealed class InventoryBackupService : IInventoryBackupService
{
    private readonly IInventoryBackupExporter backupExporter;
    private readonly IInventoryBackupClient backupClient;

    public InventoryBackupService(
        IInventoryBackupExporter backupExporter,
        IInventoryBackupClient backupClient)
    {
        this.backupExporter = backupExporter;
        this.backupClient = backupClient;
    }

    /// <inheritdoc />
    public async Task<InventoryBackupEnvelope> ExportAndUploadAsync(CancellationToken cancellationToken = default)
    {
        var backup = await backupExporter.ExportAsync(cancellationToken).ConfigureAwait(false);
        await backupClient.UploadAsync(backup, cancellationToken).ConfigureAwait(false);
        return backup;
    }
}
