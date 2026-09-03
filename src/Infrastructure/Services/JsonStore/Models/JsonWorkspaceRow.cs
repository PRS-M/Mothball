namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonWorkspaceRow
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Kind { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public long Version { get; set; }
    public Guid DefaultWarehouseId { get; set; }
    public Guid UnassignedLocationId { get; set; }
    public Guid DefaultUnitOfMeasureId { get; set; }
    public Guid DefaultStockStatusId { get; set; }
}
