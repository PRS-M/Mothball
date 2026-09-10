namespace CoreApp.Application.Abstractions.Sync;

/// <summary>Describes the delivery state of a locally persisted synchronization message.</summary>
public enum SyncOutboxStatus
{
    Pending,
    InFlight,
    Sent,
    Failed,
}

/// <summary>
/// Represents a versioned, idempotent message waiting to be sent to a synchronization service.
/// </summary>
public sealed record SyncOutboxMessage(
    Guid EventId,
    string SourceDeviceId,
    long Sequence,
    string EventType,
    int SchemaVersion,
    DateTimeOffset OccurredUtc,
    string AggregateType,
    Guid? AggregateId,
    string PayloadJson,
    SyncOutboxStatus Status = SyncOutboxStatus.Pending,
    int AttemptCount = 0,
    DateTimeOffset? NextAttemptUtc = null,
    string? LastError = null);
