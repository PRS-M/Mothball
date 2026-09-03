namespace CoreApp.Domain.Entities.WarehouseAggregate;

/// <summary>Operational warehouse site containing locations.</summary>
public sealed record Warehouse
{
    public Warehouse(Guid warehouseId, string code, string name)
    {
        WarehouseId = ValidateId(warehouseId);
        Code = ValidateText(code, nameof(code));
        Name = ValidateText(name, nameof(name));
    }

    public Guid WarehouseId { get; }
    public string Code { get; }
    public string Name { get; }

    private static Guid ValidateId(Guid value) => value == Guid.Empty ? throw new ArgumentException("Warehouse ID cannot be empty.", nameof(value)) : value;
    private static string ValidateText(string value, string parameterName) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be blank.", parameterName) : value.Trim();
}
