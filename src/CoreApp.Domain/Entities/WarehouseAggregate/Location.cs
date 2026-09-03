namespace CoreApp.Domain.Entities.WarehouseAggregate;

/// <summary>Stable address within a warehouse.</summary>
public sealed record Location
{
    public Location(Guid locationId, Guid warehouseId, string code, Guid? parentLocationId = null)
    {
        LocationId = ValidateId(locationId, nameof(locationId));
        WarehouseId = ValidateId(warehouseId, nameof(warehouseId));
        Code = ValidateCode(code);
        ParentLocationId = parentLocationId;
        if (parentLocationId == locationId) throw new ArgumentException("A location cannot be its own parent.", nameof(parentLocationId));
    }

    public Guid LocationId { get; }
    public Guid WarehouseId { get; }
    public string Code { get; }
    public Guid? ParentLocationId { get; }

    private static Guid ValidateId(Guid value, string parameterName) => value == Guid.Empty ? throw new ArgumentException("ID cannot be empty.", parameterName) : value;
    private static string ValidateCode(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Location code cannot be blank.", nameof(value)) : value.Trim();
}
