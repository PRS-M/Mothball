using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbImage : IValidatableDbModel
{
	[PrimaryKey, NotNull]
	public Guid ImageId { get; set; } = Guid.NewGuid();

    // Owner relationship (GUID)
    [Indexed]
    public Guid OwnerUniqueId { get; set; }

	[SQLite.Ignore]
	public string FileName => $"{ImageId}.jpg";

	// Not recommended for large images, but kept as optional blob for thumbnails or small data
	public byte[]? ImageData { get; set; }

    public void Validate()
    {
        if (ImageId == Guid.Empty)
        {
            throw new InvalidOperationException("Image ID cannot be empty.");
        }

        if (OwnerUniqueId == Guid.Empty)
        {
            throw new InvalidOperationException("Image owner ID cannot be empty.");
        }
    }
}
