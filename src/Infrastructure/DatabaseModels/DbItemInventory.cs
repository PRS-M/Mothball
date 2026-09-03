using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbItemInventory : IValidatableDbModel
{
    [PrimaryKey, NotNull]
    public Guid ItemId { get; set; }

    [NotNull]
    public int TotalQuantity { get; set; } = 1;

    public void Validate()
    {
        if (ItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Inventory item ID cannot be empty.");
        }

        if (TotalQuantity < 1)
        {
            throw new InvalidOperationException("Inventory total quantity must be at least one.");
        }
    }
}
