using SQLite;

namespace Infrastructure.Services.DatabaseModels;

[Table("PendingSyncOperations")]
public sealed class DbPendingSyncOperation
{
    [PrimaryKey, NotNull] public Guid OperationId { get; set; }
    [Indexed, NotNull] public Guid WorkspaceId { get; set; }
    public Guid DeviceId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public int PayloadVersion { get; set; }
    public string Payload { get; set; } = string.Empty;
    public long? BaseServerVersion { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptUtc { get; set; }
    public string? LastErrorCode { get; set; }
    public int State { get; set; }
}

[Table("EntityTombstones")]
public sealed class DbEntityTombstone
{
    [PrimaryKey, NotNull] public Guid OperationId { get; set; }
    [Indexed, NotNull] public Guid WorkspaceId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTimeOffset DeletedUtc { get; set; }
    public long? ServerRevision { get; set; }
}

[Table("WorkspaceSyncStates")]
public sealed class DbWorkspaceSyncState
{
    [PrimaryKey, NotNull] public Guid WorkspaceId { get; set; }
    public Guid DeviceId { get; set; }
    public string? LastServerCursor { get; set; }
    public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool BootstrapRequired { get; set; }
}

[Table("AppliedRemoteOperations")]
public sealed class DbAppliedRemoteOperation
{
    [PrimaryKey, NotNull] public Guid OperationId { get; set; }
    [Indexed, NotNull] public Guid WorkspaceId { get; set; }
    public long ServerRevision { get; set; }
    public DateTimeOffset AppliedUtc { get; set; }
}
