using System;
using SQLite;

namespace MothballMobile.Infrastructure.DatabaseModels;

public class DbContainer
{
	[PrimaryKey]
	[NotNull]
	public string UniqueId { get; set; } = Guid.NewGuid().ToString();

	[NotNull]
	[Indexed]
	public string Name { get; set; } = string.Empty;

	public string LocationDescription { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	// Store only the file name for the container's photo; image bytes are stored on disk
	public string? PhotoFileName { get; set; }
}
