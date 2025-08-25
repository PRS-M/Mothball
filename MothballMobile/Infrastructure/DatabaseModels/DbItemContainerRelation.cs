using System;
using SQLite;

namespace MothballMobile.Infrastructure.DatabaseModels;

public class DbItemContainerRelation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Foreign key to DbItem.UniqueId
    [Indexed]
    public string ItemId { get; set; } = string.Empty;

    // Foreign key to DbContainer.UniqueId
    [Indexed]
    public string ContainerId { get; set; } = string.Empty;
}
