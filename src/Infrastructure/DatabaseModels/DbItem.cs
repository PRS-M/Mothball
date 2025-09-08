using System;
using SQLite;

namespace MothballMobile.Infrastructure.DatabaseModels;

public class DbItem
{
	[PrimaryKey, NotNull]
	public Guid ItemId { get; set; } = Guid.NewGuid();

	[Indexed, NotNull]
	public string Name { get; set; } = string.Empty;
}
