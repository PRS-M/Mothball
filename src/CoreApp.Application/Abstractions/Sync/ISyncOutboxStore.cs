namespace CoreApp.Application.Abstractions.Sync;

/// <summary>Persists locally generated synchronization messages until remote delivery succeeds.</summary>
public interface ISyncOutboxStore
{
    /// <summary>Appends messages idempotently to the local outbox.</summary>
    Task EnqueueAsync(
        IReadOnlyCollection<SyncOutboxMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>Claims messages eligible for a delivery attempt.</summary>
    Task<IReadOnlyList<SyncOutboxMessage>> ClaimPendingAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Marks messages as successfully accepted by the remote service.</summary>
    Task MarkSentAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default);

    /// <summary>Records a failed attempt and schedules retry with backoff.</summary>
    Task MarkFailedAsync(
        IReadOnlyCollection<Guid> eventIds,
        string error,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}
