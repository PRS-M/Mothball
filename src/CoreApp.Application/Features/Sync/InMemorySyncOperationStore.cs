namespace CoreApp.Application.Features.Sync;

/// <summary>Deterministic reference store used by offline composition and orchestration tests.</summary>
public sealed class InMemorySyncOperationStore : ISyncOperationStore
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, PendingSyncOperation> operations = [];
    private readonly Dictionary<Guid, WorkspaceSyncState> states = [];
    private readonly HashSet<Guid> appliedOperations = [];
    private readonly List<EntityTombstone> tombstones = [];

    public Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(Guid workspaceId, int maxCount, CancellationToken cancellationToken = default)
    {
        if (maxCount < 1) throw new ArgumentOutOfRangeException(nameof(maxCount));
        lock (gate) return Task.FromResult<IReadOnlyList<PendingSyncOperation>>(operations.Values.Where(x => x.WorkspaceId == workspaceId && x.State == SyncOperationState.Pending).OrderBy(x => x.CreatedUtc).Take(maxCount).ToList());
    }

    public Task EnqueueAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        lock (gate) operations.TryAdd(operation.OperationId, operation);
        return Task.CompletedTask;
    }

    public Task AddTombstoneAsync(EntityTombstone tombstone, CancellationToken cancellationToken = default)
    {
        lock (gate) if (tombstones.All(x => x.OperationId != tombstone.OperationId)) tombstones.Add(tombstone);
        return Task.CompletedTask;
    }

    public Task AcknowledgeAsync(Guid workspaceId, IReadOnlyCollection<Guid> operationIds, CancellationToken cancellationToken = default)
    {
        lock (gate)
            foreach (var id in operationIds.Where(operations.ContainsKey)) operations[id] = operations[id] with { State = SyncOperationState.Acknowledged };
        return Task.CompletedTask;
    }

    public Task RecordFailureAsync(Guid workspaceId, Guid operationId, string errorCode, CancellationToken cancellationToken = default)
    {
        lock (gate)
            if (operations.TryGetValue(operationId, out var operation)) operations[operationId] = operation with { State = SyncOperationState.Failed, LastErrorCode = errorCode, AttemptCount = operation.AttemptCount + 1, LastAttemptUtc = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task ApplyRemotePageAsync(Guid workspaceId, SyncChangePage page, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            foreach (var change in page.Changes) appliedOperations.Add(change.OperationId);
            if (states.TryGetValue(workspaceId, out var state)) states[workspaceId] = state with { LastServerCursor = page.NextCursor };
        }
        return Task.CompletedTask;
    }

    public Task<WorkspaceSyncState?> GetSyncStateAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        lock (gate) return Task.FromResult(states.GetValueOrDefault(workspaceId));
    }

    public Task SaveSyncStateAsync(WorkspaceSyncState state, CancellationToken cancellationToken = default)
    {
        lock (gate) states[state.WorkspaceId] = state;
        return Task.CompletedTask;
    }
}
