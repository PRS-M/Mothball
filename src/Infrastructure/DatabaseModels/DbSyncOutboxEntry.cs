using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public sealed class DbSyncOutboxEntry
{
    [PrimaryKey]
    public Guid EventId { get; set; }
    public string SourceDeviceId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string OccurredUtc { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public Guid? AggregateId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public int Status { get; set; }
    public int AttemptCount { get; set; }
    public string? NextAttemptUtc { get; set; }
    public string? LastError { get; set; }
}
