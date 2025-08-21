using System;
using SQLite;

namespace MothballMobile.Infrastructure.DatabaseModels;

public class DbPhoto
{
	[PrimaryKey]
	[AutoIncrement]
	public int Id { get; set; }

    // Owner relationship (GUID)
    [Indexed]
    public string? OwnerUniqueId { get; set; }

	[NotNull]
	public string FileName { get; set; } = string.Empty;

	// Not recommended for large images, but kept as optional blob for thumbnails or small data
	public byte[]? ImageData { get; set; }
}
