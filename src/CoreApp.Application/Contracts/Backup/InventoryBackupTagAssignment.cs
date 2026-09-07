using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Contracts.Backup;

/// <summary>
/// Describes a tag assignment included in an inventory backup.
/// </summary>
public sealed record InventoryBackupTagAssignment
{
    public Guid TagId { get; init; }
    public Guid TargetId { get; init; }
    public TagTargetType TargetType { get; init; }
}
