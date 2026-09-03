namespace CoreApp.Application.Features.Sync;

/// <summary>Lifecycle state of a locally queued synchronization operation.</summary>
public enum SyncOperationState { Pending, InFlight, Acknowledged, Failed }

/// <summary>Durable envelope for a local mutation awaiting synchronization.</summary>
public sealed record PendingSyncOperation(
    Guid OperationId,
    Guid WorkspaceId,
    Guid DeviceId,
    string AggregateType,
    Guid AggregateId,
    string OperationType,
    int PayloadVersion,
    string Payload,
    long? BaseServerVersion,
    DateTimeOffset CreatedUtc,
    int AttemptCount = 0,
    DateTimeOffset? LastAttemptUtc = null,
    string? LastErrorCode = null,
    SyncOperationState State = SyncOperationState.Pending);

/// <summary>Durable deletion marker that can be replayed by another device.</summary>
public sealed record EntityTombstone(Guid WorkspaceId, string EntityType, Guid EntityId, DateTimeOffset DeletedUtc, Guid OperationId, long? ServerRevision = null);

/// <summary>Per-workspace synchronization cursor and status.</summary>
public sealed record WorkspaceSyncState(Guid WorkspaceId, Guid DeviceId, string? LastServerCursor, DateTimeOffset? LastSuccessfulSyncUtc, string Status, bool BootstrapRequired);

/// <summary>Records a remote operation already applied locally.</summary>
public sealed record AppliedRemoteOperation(Guid WorkspaceId, Guid OperationId, long ServerRevision, DateTimeOffset AppliedUtc);

/// <summary>Push response with acknowledgements and structured conflicts.</summary>
public sealed record SyncPushResult(IReadOnlyList<Guid> AcknowledgedOperationIds, IReadOnlyList<SyncConflict> Conflicts);

/// <summary>Structured optimistic-concurrency conflict.</summary>
public sealed record SyncConflict(Guid OperationId, string Code, string Message);

/// <summary>One ordered remote change-feed entry.</summary>
public sealed record SyncChange(long ServerRevision, Guid OperationId, Guid WorkspaceId, string AggregateType, Guid AggregateId, string OperationType, int PayloadVersion, string Payload, bool IsTombstone = false);

/// <summary>Page of ordered remote changes.</summary>
public sealed record SyncChangePage(IReadOnlyList<SyncChange> Changes, string? NextCursor, bool HasMore, bool BootstrapRequired = false);

/// <summary>Complete bootstrap response for a new or expired client cursor.</summary>
public sealed record SyncBootstrapResult(string SnapshotId, long ServerRevision, string SnapshotPayload, string? ContinuationCursor);

/// <summary>Backend-neutral synchronization transport.</summary>
public interface ISyncClient
{
    Task<SyncBootstrapResult> BootstrapAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<SyncPushResult> PushAsync(Guid workspaceId, IReadOnlyList<PendingSyncOperation> operations, CancellationToken cancellationToken = default);
    Task<SyncChangePage> PullAsync(Guid workspaceId, string? cursor, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>Atomic local persistence seam for synchronization state.</summary>
public interface ISyncOperationStore
{
    Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(Guid workspaceId, int maxCount, CancellationToken cancellationToken = default);
    Task EnqueueAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default);
    Task AddTombstoneAsync(EntityTombstone tombstone, CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(Guid workspaceId, IReadOnlyCollection<Guid> operationIds, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(Guid workspaceId, Guid operationId, string errorCode, CancellationToken cancellationToken = default);
    Task ApplyRemotePageAsync(Guid workspaceId, SyncChangePage page, CancellationToken cancellationToken = default);
    Task<WorkspaceSyncState?> GetSyncStateAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task SaveSyncStateAsync(WorkspaceSyncState state, CancellationToken cancellationToken = default);
}
