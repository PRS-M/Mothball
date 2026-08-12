using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IInventoryBackupClient
{
    Task UploadAsync(InventoryBackupEnvelope backup, CancellationToken cancellationToken = default);
}
