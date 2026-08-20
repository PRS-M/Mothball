namespace CoreApp.Application.Contracts.Backup;

public sealed record InventoryBackupImageRef
{
    public Guid ImageId { get; init; }
    public Guid OwnerId { get; init; }
    public InventoryBackupOwnerType OwnerType { get; init; }
    public string FileName { get; init; } = string.Empty;
}