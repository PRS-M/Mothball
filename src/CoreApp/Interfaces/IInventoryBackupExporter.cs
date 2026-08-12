using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IInventoryBackupExporter
{
    Task<InventoryBackupEnvelope> ExportAsync(CancellationToken cancellationToken = default);

    Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default);
}
