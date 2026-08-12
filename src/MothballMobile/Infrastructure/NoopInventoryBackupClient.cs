using CoreApp.Contracts;
using CoreApp.Interfaces;

namespace MothballMobile.Infrastructure;

public sealed class NoopInventoryBackupClient : IInventoryBackupClient
{
    public Task UploadAsync(InventoryBackupEnvelope backup, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
