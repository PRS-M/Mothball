namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonCanonicalBalanceRow
{
    public Guid WorkspaceId { get; set; }
    public Guid ItemId { get; set; }
    public Guid PlacementId { get; set; }
    public int OnHandQuantity { get; set; }
    public long Version { get; set; }
}

public sealed class JsonCanonicalMovementRow
{
    public Guid MovementId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ItemId { get; set; }
    public int Type { get; set; }
    public int Quantity { get; set; }
    public Guid? SourcePlacementId { get; set; }
    public Guid? DestinationPlacementId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredUtc { get; set; }
}
