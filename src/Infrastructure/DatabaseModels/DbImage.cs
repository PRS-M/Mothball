using System;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbImage
{
	[PrimaryKey, NotNull]
	public Guid ImageId { get; set; } = Guid.NewGuid();

    // Owner relationship (GUID)
    [Indexed]
    public Guid OwnerUniqueId { get; set; }

	[Ignore]
	public string FileName => $"{ImageId}.jpg";

	// Not recommended for large images, but kept as optional blob for thumbnails or small data
	public byte[]? ImageData { get; set; }
}
