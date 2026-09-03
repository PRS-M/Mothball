namespace CoreApp.Application.Features.Sync;

/// <summary>Coordinates durable push/pull ordering without depending on a transport.</summary>
public sealed class SyncOrchestrator(ISyncOperationStore store, ISyncClient client)
{
    /// <summary>Pushes pending operations, then applies remote pages before advancing the cursor.</summary>
    public async Task SynchronizeAsync(Guid workspaceId, int batchSize = 50, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        var state = await store.GetSyncStateAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        if (state?.BootstrapRequired == true)
        {
            var bootstrap = await client.BootstrapAsync(workspaceId, cancellationToken).ConfigureAwait(false);
            await store.ApplyRemotePageAsync(workspaceId, new SyncChangePage([], bootstrap.ContinuationCursor, false), cancellationToken).ConfigureAwait(false);
            state = new WorkspaceSyncState(workspaceId, state.DeviceId, bootstrap.ContinuationCursor, DateTimeOffset.UtcNow, "Ready", false);
            await store.SaveSyncStateAsync(state, cancellationToken).ConfigureAwait(false);
        }

        var pending = await store.GetPendingAsync(workspaceId, batchSize, cancellationToken).ConfigureAwait(false);
        if (pending.Count > 0)
        {
            var pushed = await client.PushAsync(workspaceId, pending, cancellationToken).ConfigureAwait(false);
            await store.AcknowledgeAsync(workspaceId, pushed.AcknowledgedOperationIds, cancellationToken).ConfigureAwait(false);
            foreach (var conflict in pushed.Conflicts)
                await store.RecordFailureAsync(workspaceId, conflict.OperationId, conflict.Code, cancellationToken).ConfigureAwait(false);
        }

        var cursor = state?.LastServerCursor;
        while (true)
        {
            var page = await client.PullAsync(workspaceId, cursor, pageSize, cancellationToken).ConfigureAwait(false);
            if (page.BootstrapRequired)
            {
                await store.SaveSyncStateAsync(new WorkspaceSyncState(workspaceId, state?.DeviceId ?? Guid.Empty, cursor, null, "BootstrapRequired", true), cancellationToken).ConfigureAwait(false);
                return;
            }
            await store.ApplyRemotePageAsync(workspaceId, page, cancellationToken).ConfigureAwait(false);
            cursor = page.NextCursor;
            if (!page.HasMore) break;
        }

        await store.SaveSyncStateAsync(new WorkspaceSyncState(workspaceId, state?.DeviceId ?? Guid.Empty, cursor, DateTimeOffset.UtcNow, "Ready", false), cancellationToken).ConfigureAwait(false);
    }
}
