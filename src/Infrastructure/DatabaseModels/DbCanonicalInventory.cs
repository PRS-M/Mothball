using SQLite;

namespace Infrastructure.Services.DatabaseModels;

[Table("InventoryBalances")]
public sealed class DbInventoryBalance
{
    [PrimaryKey, NotNull] public string BalanceId { get; set; } = string.Empty;
    [Indexed, NotNull] public Guid WorkspaceId { get; set; }
    [Indexed, NotNull] public Guid ItemId { get; set; }
    [Indexed, NotNull] public Guid PlacementId { get; set; }
    public int OnHandQuantity { get; set; }
    public long Version { get; set; }
}

[Table("InventoryMovements")]
public sealed class DbInventoryMovement
{
    [PrimaryKey, NotNull] public Guid MovementId { get; set; }
    [Indexed, NotNull] public Guid WorkspaceId { get; set; }
    [Indexed, NotNull] public Guid ItemId { get; set; }
    public int Type { get; set; }
    public int Quantity { get; set; }
    public Guid? SourcePlacementId { get; set; }
    public Guid? DestinationPlacementId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredUtc { get; set; }
}
