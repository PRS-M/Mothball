namespace CoreApp.Application.Contracts.Backup;

public sealed record InventoryBackupData
{
    public List<InventoryBackupContainer> Containers { get; init; } = [];
    public List<InventoryBackupItem> Items { get; init; } = [];
    public List<InventoryBackupRelation> Relations { get; init; } = [];
    public List<InventoryBackupImageRef> Images { get; init; } = [];
    public List<InventoryBackupTag> Tags { get; init; } = [];
    public List<InventoryBackupTagAssignment> TagAssignments { get; init; } = [];
}
