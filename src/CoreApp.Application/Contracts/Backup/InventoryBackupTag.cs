namespace CoreApp.Application.Contracts.Backup;

/// <summary>
/// Describes a tag included in an inventory backup.
/// </summary>
public sealed record InventoryBackupTag
{
    public Guid TagId { get; init; }
    public string Name { get; init; } = string.Empty;
}
