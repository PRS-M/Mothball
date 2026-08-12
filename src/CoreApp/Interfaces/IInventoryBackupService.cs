using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IInventoryBackupService
{
    Task<InventoryBackupEnvelope> ExportAndUploadAsync(CancellationToken cancellationToken = default);
}
