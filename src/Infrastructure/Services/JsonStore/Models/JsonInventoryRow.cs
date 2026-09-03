namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonInventoryRow
{
    public Guid ItemId { get; set; }
    public int TotalQuantity { get; set; } = 1;
}
