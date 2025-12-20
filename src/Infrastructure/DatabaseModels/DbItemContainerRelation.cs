using System;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbItemContainerRelation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Foreign key to DbItem.UniqueId
    [Indexed, ForeignKey(nameof(DbItem))]
    public Guid ItemId { get; set; }

    // Foreign key to DbContainer.UniqueId
    [Indexed, ForeignKey(nameof(DbContainer))]
    public Guid ContainerId { get; set; }

    [NotNull]
    public int Quantity { get; set; } = 1;
}
