namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonSyncOutboxRow
{
    public Guid EventId { get; set; }
    public string SourceDeviceId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid? AggregateId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public int Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptUtc { get; set; }
    public string? LastError { get; set; }
}
