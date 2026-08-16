using System;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbItemInventory
{
    [PrimaryKey, NotNull]
    public Guid ItemId { get; set; }

    [NotNull]
    public int TotalQuantity { get; set; } = 1;
}
