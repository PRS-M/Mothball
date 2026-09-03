using CoreApp.Application.Features.Sync;

namespace Infrastructure.Services.JsonStore;

/// <summary>JSON-backed durable synchronization outbox and cursor store.</summary>
public sealed class JsonSyncOperationStore(JsonInventoryStore store) : ISyncOperationStore
{
    public async Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(Guid workspaceId, int maxCount, CancellationToken cancellationToken = default)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.PendingSyncOperations.Where(x => x.WorkspaceId == workspaceId && x.State == SyncOperationState.Pending).OrderBy(x => x.CreatedUtc).Take(maxCount).ToList();
    }

    public Task EnqueueAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default)
        => store.UpdateAsync(state => { if (!state.PendingSyncOperations.Any(x => x.OperationId == operation.OperationId)) state.PendingSyncOperations.Add(operation); return Task.CompletedTask; });

    public Task AddTombstoneAsync(EntityTombstone tombstone, CancellationToken cancellationToken = default)
        => store.UpdateAsync(state => { if (state.EntityTombstones.All(x => x.OperationId != tombstone.OperationId)) state.EntityTombstones.Add(tombstone); return Task.CompletedTask; });

    public Task AcknowledgeAsync(Guid workspaceId, IReadOnlyCollection<Guid> operationIds, CancellationToken cancellationToken = default)
        => store.UpdateAsync(state => { state.PendingSyncOperations = state.PendingSyncOperations.Select(x => x.WorkspaceId == workspaceId && operationIds.Contains(x.OperationId) ? x with { State = SyncOperationState.Acknowledged } : x).ToList(); return Task.CompletedTask; });

    public Task RecordFailureAsync(Guid workspaceId, Guid operationId, string errorCode, CancellationToken cancellationToken = default)
        => store.UpdateAsync(state => { state.PendingSyncOperations = state.PendingSyncOperations.Select(x => x.WorkspaceId == workspaceId && x.OperationId == operationId ? x with { State = SyncOperationState.Failed, LastErrorCode = errorCode, AttemptCount = x.AttemptCount + 1, LastAttemptUtc = DateTimeOffset.UtcNow } : x).ToList(); return Task.CompletedTask; });

    public Task ApplyRemotePageAsync(Guid workspaceId, SyncChangePage page, CancellationToken cancellationToken = default)
        => store.UpdateAsync(state =>
        {
            foreach (var change in page.Changes.Where(x => x.WorkspaceId == workspaceId && state.AppliedRemoteOperations.All(y => y.OperationId != x.OperationId)))
                state.AppliedRemoteOperations.Add(new AppliedRemoteOperation(workspaceId, change.OperationId, change.ServerRevision, DateTimeOffset.UtcNow));
            var existing = state.WorkspaceSyncStates.FirstOrDefault(x => x.WorkspaceId == workspaceId);
            var updated = new WorkspaceSyncState(workspaceId, existing?.DeviceId ?? Guid.Empty, page.NextCursor, existing?.LastSuccessfulSyncUtc, existing?.Status ?? "Applying", existing?.BootstrapRequired ?? false);
            state.WorkspaceSyncStates = state.WorkspaceSyncStates.Where(x => x.WorkspaceId != workspaceId).Append(updated).ToList();
            return Task.CompletedTask;
        });

    public async Task<WorkspaceSyncState?> GetSyncStateAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => (await store.LoadAsync().ConfigureAwait(false)).WorkspaceSyncStates.FirstOrDefault(x => x.WorkspaceId == workspaceId);

    public Task SaveSyncStateAsync(WorkspaceSyncState state, CancellationToken cancellationToken = default)
        => store.UpdateAsync(current => { current.WorkspaceSyncStates = current.WorkspaceSyncStates.Where(x => x.WorkspaceId != state.WorkspaceId).Append(state).ToList(); return Task.CompletedTask; });
}
