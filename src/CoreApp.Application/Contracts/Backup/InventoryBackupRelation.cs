namespace CoreApp.Application.Contracts.Backup;

public sealed record InventoryBackupRelation
{
    public Guid ContainerId { get; init; }
    public Guid ItemId { get; init; }
    public int Quantity { get; init; }
}