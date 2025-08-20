using System;
using SQLite;

namespace MothballMobile.Infrastructure.DatabaseModels;

public class DbPhoto
{
	[PrimaryKey]
	[AutoIncrement]
	public int Id { get; set; }

	// Optional owner relationships
	[Indexed]
	public string? ContainerId { get; set; }

	[Indexed]
	public string? ItemId { get; set; }

	[NotNull]
	public string FileName { get; set; } = string.Empty;

	// Not recommended for large images, but kept as optional blob for thumbnails or small data
	public byte[]? ImageData { get; set; }
}
