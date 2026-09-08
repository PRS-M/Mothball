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
	public string FileName => string.IsNullOrWhiteSpace(StoredFileName) ? $"{ImageId}.jpg" : StoredFileName;

    /// <summary>
    /// Optional stored filename used when multiple image records share one physical asset.
    /// </summary>
    public string? StoredFileName { get; set; }

    /// <summary>
    /// Indicates that deleting this metadata row must not delete its physical file.
    /// </summary>
    public bool IsSharedAsset { get; set; }

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
