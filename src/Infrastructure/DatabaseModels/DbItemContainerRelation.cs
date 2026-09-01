using System;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public class DbItemContainerRelation : IValidatableDbModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Foreign key to DbItem.UniqueId
    [Indexed, Indexed("UX_DbItemContainerRelation_ItemId_ContainerId", 1, Unique = true), ForeignKey(nameof(DbItem))]
    public Guid ItemId { get; set; }

    // Foreign key to DbContainer.UniqueId
    [Indexed, Indexed("UX_DbItemContainerRelation_ItemId_ContainerId", 2, Unique = true), ForeignKey(nameof(DbContainer))]
    public Guid ContainerId { get; set; }

    [NotNull]
    public int Quantity { get; set; } = 1;

    public void Validate()
    {
        if (ItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Relation item ID cannot be empty.");
        }

        if (ContainerId == Guid.Empty)
        {
            throw new InvalidOperationException("Relation container ID cannot be empty.");
        }

        if (Quantity <= 0)
        {
            throw new InvalidOperationException("Relation quantity must be positive.");
        }
    }
}
