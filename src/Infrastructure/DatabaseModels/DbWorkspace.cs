using SQLite;

namespace Infrastructure.Services.DatabaseModels;

[Table("Workspaces")]
public sealed class DbWorkspace : IValidatableDbModel
{
    [PrimaryKey, NotNull] public Guid WorkspaceId { get; set; }
    [NotNull] public string Name { get; set; } = string.Empty;
    public int Kind { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public long Version { get; set; }
    public void Validate()
    {
        if (WorkspaceId == Guid.Empty) throw new InvalidOperationException("Workspace ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("Workspace name cannot be empty.");
    }
}

[Table("WorkspaceDefaults")]
public sealed class DbWorkspaceDefaults : IValidatableDbModel
{
    [PrimaryKey, NotNull] public Guid WorkspaceId { get; set; }
    [NotNull] public Guid DefaultWarehouseId { get; set; }
    [NotNull] public Guid UnassignedLocationId { get; set; }
    [NotNull] public Guid DefaultUnitOfMeasureId { get; set; }
    [NotNull] public Guid DefaultStockStatusId { get; set; }
    public void Validate()
    {
        if (WorkspaceId == Guid.Empty) throw new InvalidOperationException("Workspace ID cannot be empty.");
    }
}
