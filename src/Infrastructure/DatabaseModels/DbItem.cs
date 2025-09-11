using System;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbItem
{
	[PrimaryKey, NotNull]
	public Guid ItemId { get; set; } = Guid.NewGuid();

	[Indexed, NotNull]
	public string Name { get; set; } = string.Empty;
}
