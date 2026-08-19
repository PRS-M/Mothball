namespace CoreApp.Contracts.Backup;

public sealed record InventoryBackupItem
{
    public Guid ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int TotalQuantity { get; init; } = 1;
}