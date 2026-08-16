using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Export;

public interface IInventoryBackupService
{
    Task<InventoryBackupEnvelope> ExportAndUploadAsync(CancellationToken cancellationToken = default);
}
