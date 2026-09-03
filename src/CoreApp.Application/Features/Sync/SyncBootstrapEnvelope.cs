namespace CoreApp.Application.Features.Sync;

/// <summary>Versioned complete snapshot exchanged during synchronization bootstrap.</summary>
public sealed record SyncBootstrapEnvelope(
    int PayloadVersion,
    int SchemaVersion,
    Guid WorkspaceId,
    string SnapshotId,
    long ServerRevision,
    bool IsCompleteSnapshot,
    string Source,
    string SnapshotPayload,
    string? Checksum);

/// <summary>Classifies synchronization failures for presentation and retry policy.</summary>
public enum SyncErrorKind { Authentication, Authorization, Validation, Conflict, RateLimited, Unavailable, Transport, Unknown }

/// <summary>Transport-independent synchronization exception.</summary>
public sealed class SyncException(SyncErrorKind kind, string message, Exception? innerException = null) : Exception(message, innerException)
{
    public SyncErrorKind Kind { get; } = kind;
}
