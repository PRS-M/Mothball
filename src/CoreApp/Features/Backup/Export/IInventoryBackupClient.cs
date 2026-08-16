using CoreApp.Contracts;

namespace CoreApp.Features.Backup.Export;

public interface IInventoryBackupClient
{
    Task UploadAsync(InventoryBackupEnvelope backup, CancellationToken cancellationToken = default);
}
