using System;
using SQLite;

namespace MothballMobile.Infrastructure.DatabaseModels;

public class DbContainer
{
	[PrimaryKey, NotNull]
	public Guid ContainerId { get; set; } = Guid.NewGuid();

	[Indexed, NotNull]
	public string Name { get; set; } = string.Empty;

	public string LocationDescription { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;
}
