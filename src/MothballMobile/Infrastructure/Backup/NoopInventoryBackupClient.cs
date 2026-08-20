using CoreApp.Application.Contracts;

namespace MothballMobile.Infrastructure.Backup;

public sealed class NoopInventoryBackupClient : IInventoryBackupClient
{
    /// <inheritdoc />
    public Task UploadAsync(InventoryBackupEnvelope backup, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
