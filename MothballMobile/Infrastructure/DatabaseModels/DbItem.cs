using System;
using SQLite;

namespace MothballMobile.Infrastructure.DatabaseModels;

public class DbItem
{
	[PrimaryKey]
	[NotNull]
	public string UniqueId { get; set; } = Guid.NewGuid().ToString();

	[NotNull]
	[Indexed]
	public string Name { get; set; } = string.Empty;
}
