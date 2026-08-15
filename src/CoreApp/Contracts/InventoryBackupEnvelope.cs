namespace CoreApp.Contracts;

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

public sealed record InventoryBackupData
{
    public List<InventoryBackupContainer> Containers { get; init; } = [];
    public List<InventoryBackupItem> Items { get; init; } = [];
    public List<InventoryBackupRelation> Relations { get; init; } = [];
    public List<InventoryBackupImageRef> Images { get; init; } = [];
}

public sealed record InventoryBackupContainer
{
    public Guid ContainerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

public sealed record InventoryBackupItem
{
    public Guid ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int TotalQuantity { get; init; } = 1;
}

public sealed record InventoryBackupRelation
{
    public Guid ContainerId { get; init; }
    public Guid ItemId { get; init; }
    public int Quantity { get; init; }
}

public enum InventoryBackupOwnerType
{
    Container,
    Item,
}

public sealed record InventoryBackupImageRef
{
    public Guid ImageId { get; init; }
    public Guid OwnerId { get; init; }
    public InventoryBackupOwnerType OwnerType { get; init; }
    public string FileName { get; init; } = string.Empty;
}
