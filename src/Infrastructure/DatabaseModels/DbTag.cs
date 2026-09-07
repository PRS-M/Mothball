using SQLite;

namespace Infrastructure.Services.DatabaseModels;

/// <summary>
/// SQLite representation of a reusable inventory tag.
/// </summary>
public class DbTag : IValidatableDbModel
{
    [PrimaryKey, NotNull]
    public Guid TagId { get; set; } = Guid.NewGuid();

    [NotNull]
    public string Name { get; set; } = string.Empty;

    [NotNull, Indexed("UX_DbTag_NormalizedName", 1, Unique = true)]
    public string NormalizedName { get; set; } = string.Empty;

    public void Validate()
    {
        if (TagId == Guid.Empty)
        {
            throw new InvalidOperationException("Tag ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(NormalizedName))
        {
            throw new InvalidOperationException("Tag name cannot be empty.");
        }
    }
}
