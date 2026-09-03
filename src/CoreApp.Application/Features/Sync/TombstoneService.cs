namespace CoreApp.Application.Features.Sync;

/// <summary>Records synchronized deletion markers and their outbox operation.</summary>
public sealed class TombstoneService(ISyncOperationStore store)
{
    /// <summary>Creates an idempotent tombstone for an archived entity.</summary>
    public async Task<EntityTombstone> CreateAsync(Guid workspaceId, string entityType, Guid entityId, Guid operationId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty) throw new ArgumentException("Workspace ID cannot be empty.", nameof(workspaceId));
        if (entityId == Guid.Empty) throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));
        if (operationId == Guid.Empty) throw new ArgumentException("Operation ID cannot be empty.", nameof(operationId));
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
        var tombstone = new EntityTombstone(workspaceId, entityType.Trim(), entityId, DateTimeOffset.UtcNow, operationId);
        await store.AddTombstoneAsync(tombstone, cancellationToken).ConfigureAwait(false);
        await store.EnqueueAsync(new PendingSyncOperation(operationId, workspaceId, deviceId, entityType.Trim(), entityId, "Delete", 1, "{}", null, tombstone.DeletedUtc), cancellationToken).ConfigureAwait(false);
        return tombstone;
    }
}
