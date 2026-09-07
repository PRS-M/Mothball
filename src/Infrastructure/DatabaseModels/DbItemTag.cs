using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

/// <summary>
/// SQLite many-to-many assignment between an item and a tag.
/// </summary>
public class DbItemTag : IValidatableDbModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, Indexed("UX_DbItemTag_ItemId_TagId", 1, Unique = true), ForeignKey(nameof(DbItem))]
    public Guid ItemId { get; set; }

    [Indexed, Indexed("UX_DbItemTag_ItemId_TagId", 2, Unique = true), ForeignKey(nameof(DbTag))]
    public Guid TagId { get; set; }

    public void Validate()
    {
        if (ItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Item tag assignment item ID cannot be empty.");
        }

        if (TagId == Guid.Empty)
        {
            throw new InvalidOperationException("Item tag assignment tag ID cannot be empty.");
        }
    }
}
