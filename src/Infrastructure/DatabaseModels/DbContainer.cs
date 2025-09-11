using System;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbContainer
{
	[PrimaryKey, NotNull]
	public Guid ContainerId { get; set; } = Guid.NewGuid();

	[Indexed, NotNull]
	public string Name { get; set; } = string.Empty;

	public string Notes { get; set; } = string.Empty;
}
