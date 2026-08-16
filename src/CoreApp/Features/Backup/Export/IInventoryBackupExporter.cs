using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Export;

public interface IInventoryBackupExporter
{
    Task<InventoryBackupEnvelope> ExportAsync(CancellationToken cancellationToken = default);

    Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default);

    Task<byte[]> ExportAsZipAsync(CancellationToken cancellationToken = default);
}
