namespace CoreApp.Application.Contracts.Backup;

public sealed record InventoryBackupEnvelope
{
    public const int CurrentPayloadVersion = 1;
    public int PayloadVersion { get; init; } = CurrentPayloadVersion;
    public int SchemaVersion { get; init; } = 1;
    public DateTimeOffset CreatedUtc { get; init; }
    public string Source { get; init; } = "MothballMobile";
    public InventoryBackupIntegrity Integrity { get; init; } = new();
    public InventoryBackupData Data { get; init; } = new();
}