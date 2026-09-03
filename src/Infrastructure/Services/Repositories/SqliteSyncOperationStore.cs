using CoreApp.Application.Features.Sync;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Repositories;

/// <summary>SQLite-backed durable synchronization outbox and cursor store.</summary>
public sealed class SqliteSyncOperationStore(MothballDatabase database) : ISyncOperationStore
{
    public async Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(Guid workspaceId, int maxCount, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        var rows = await database.Connection.Table<DbPendingSyncOperation>().Where(x => x.WorkspaceId == workspaceId && x.State == (int)SyncOperationState.Pending).Take(maxCount).ToListAsync().ConfigureAwait(false);
        return rows.OrderBy(x => x.CreatedUtc).Select(Map).ToList();
    }

    public async Task EnqueueAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await database.Connection.InsertOrReplaceAsync(ToRow(operation)).ConfigureAwait(false);
    }

    public async Task AddTombstoneAsync(EntityTombstone tombstone, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await database.Connection.InsertOrReplaceAsync(new DbEntityTombstone { OperationId = tombstone.OperationId, WorkspaceId = tombstone.WorkspaceId, EntityType = tombstone.EntityType, EntityId = tombstone.EntityId, DeletedUtc = tombstone.DeletedUtc, ServerRevision = tombstone.ServerRevision }).ConfigureAwait(false);
    }

    public async Task AcknowledgeAsync(Guid workspaceId, IReadOnlyCollection<Guid> operationIds, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            foreach (var id in operationIds)
            {
                var row = connection.Find<DbPendingSyncOperation>(id);
                if (row is not null && row.WorkspaceId == workspaceId)
                {
                    row.State = (int)SyncOperationState.Acknowledged;
                    connection.Update(row);
                }
            }
        }).ConfigureAwait(false);
    }

    public async Task RecordFailureAsync(Guid workspaceId, Guid operationId, string errorCode, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        var row = await database.Connection.FindAsync<DbPendingSyncOperation>(operationId).ConfigureAwait(false);
        if (row is null || row.WorkspaceId != workspaceId) return;
        row.State = (int)SyncOperationState.Failed;
        row.LastErrorCode = errorCode;
        row.AttemptCount++;
        row.LastAttemptUtc = DateTimeOffset.UtcNow;
        await database.Connection.UpdateAsync(row).ConfigureAwait(false);
    }

    public async Task ApplyRemotePageAsync(Guid workspaceId, SyncChangePage page, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            foreach (var change in page.Changes)
            {
                if (connection.Find<DbAppliedRemoteOperation>(change.OperationId) is null)
                    connection.Insert(new DbAppliedRemoteOperation { OperationId = change.OperationId, WorkspaceId = workspaceId, ServerRevision = change.ServerRevision, AppliedUtc = DateTimeOffset.UtcNow });
            }
            var state = connection.Find<DbWorkspaceSyncState>(workspaceId) ?? new DbWorkspaceSyncState { WorkspaceId = workspaceId };
            state.LastServerCursor = page.NextCursor;
            connection.InsertOrReplace(state);
        }).ConfigureAwait(false);
    }

    public async Task<WorkspaceSyncState?> GetSyncStateAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        var row = await database.Connection.FindAsync<DbWorkspaceSyncState>(workspaceId).ConfigureAwait(false);
        return row is null ? null : new WorkspaceSyncState(row.WorkspaceId, row.DeviceId, row.LastServerCursor, row.LastSuccessfulSyncUtc, row.Status, row.BootstrapRequired);
    }

    public async Task SaveSyncStateAsync(WorkspaceSyncState state, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await database.Connection.InsertOrReplaceAsync(new DbWorkspaceSyncState { WorkspaceId = state.WorkspaceId, DeviceId = state.DeviceId, LastServerCursor = state.LastServerCursor, LastSuccessfulSyncUtc = state.LastSuccessfulSyncUtc, Status = state.Status, BootstrapRequired = state.BootstrapRequired }).ConfigureAwait(false);
    }

    private static PendingSyncOperation Map(DbPendingSyncOperation x) => new(x.OperationId, x.WorkspaceId, x.DeviceId, x.AggregateType, x.AggregateId, x.OperationType, x.PayloadVersion, x.Payload, x.BaseServerVersion, x.CreatedUtc, x.AttemptCount, x.LastAttemptUtc, x.LastErrorCode, (SyncOperationState)x.State);
    private static DbPendingSyncOperation ToRow(PendingSyncOperation x) => new() { OperationId = x.OperationId, WorkspaceId = x.WorkspaceId, DeviceId = x.DeviceId, AggregateType = x.AggregateType, AggregateId = x.AggregateId, OperationType = x.OperationType, PayloadVersion = x.PayloadVersion, Payload = x.Payload, BaseServerVersion = x.BaseServerVersion, CreatedUtc = x.CreatedUtc, AttemptCount = x.AttemptCount, LastAttemptUtc = x.LastAttemptUtc, LastErrorCode = x.LastErrorCode, State = (int)x.State };
}
