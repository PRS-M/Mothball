using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace Infrastructure.Services.DatabaseModels;

/// <summary>
/// SQLite many-to-many assignment between a container and a tag.
/// </summary>
public class DbContainerTag : IValidatableDbModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, Indexed("UX_DbContainerTag_ContainerId_TagId", 1, Unique = true), ForeignKey(nameof(DbContainer))]
    public Guid ContainerId { get; set; }

    [Indexed, Indexed("UX_DbContainerTag_ContainerId_TagId", 2, Unique = true), ForeignKey(nameof(DbTag))]
    public Guid TagId { get; set; }

    public void Validate()
    {
        if (ContainerId == Guid.Empty)
        {
            throw new InvalidOperationException("Container tag assignment container ID cannot be empty.");
        }

        if (TagId == Guid.Empty)
        {
            throw new InvalidOperationException("Container tag assignment tag ID cannot be empty.");
        }
    }
}
