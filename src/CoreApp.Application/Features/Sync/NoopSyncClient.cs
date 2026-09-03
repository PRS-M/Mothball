namespace CoreApp.Application.Features.Sync;

/// <summary>Offline transport that acknowledges no work and exposes an empty change feed.</summary>
public sealed class NoopSyncClient : ISyncClient
{
    public Task<SyncBootstrapResult> BootstrapAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult(new SyncBootstrapResult(Guid.NewGuid().ToString("N"), 0, "{}", null));

    public Task<SyncPushResult> PushAsync(Guid workspaceId, IReadOnlyList<PendingSyncOperation> operations, CancellationToken cancellationToken = default)
        => Task.FromResult(new SyncPushResult([], []));

    public Task<SyncChangePage> PullAsync(Guid workspaceId, string? cursor, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new SyncChangePage([], cursor, false));
}
