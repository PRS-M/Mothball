using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbContainer : IValidatableDbModel
{
	[PrimaryKey, NotNull]
	public Guid ContainerId { get; set; } = Guid.NewGuid();

	[Indexed, NotNull]
	public string Name { get; set; } = string.Empty;

	public string Notes { get; set; } = string.Empty;

    public string BarcodeValue { get; set; } = string.Empty;

    public int? BarcodeSymbology { get; set; }

    public void Validate()
    {
        if (ContainerId == Guid.Empty)
        {
            throw new InvalidOperationException("Container ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Container name cannot be empty.");
        }
    }
}
