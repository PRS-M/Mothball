using System;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbItem : IValidatableDbModel
{
	[PrimaryKey, NotNull]
	public Guid ItemId { get; set; } = Guid.NewGuid();

	[Indexed, NotNull]
	public string Name { get; set; } = string.Empty;

	[NotNull]
	public string Description { get; set; } = string.Empty;

    public string BarcodeValue { get; set; } = string.Empty;

    public int? BarcodeSymbology { get; set; }

    public void Validate()
    {
        if (ItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Item ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Item name cannot be empty.");
        }
    }
}
