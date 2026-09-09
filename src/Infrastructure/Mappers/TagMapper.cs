using CoreApp.Domain.Entities.TagAggregate;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Mappers;

public static class TagMapper
{
    public static DbTag ToDb(this Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return new DbTag
        {
            TagId = tag.TagId,
            Name = tag.Name.Value,
            NormalizedName = tag.Name.NormalizedValue,
        };
    }

    public static Tag ToDomain(this DbTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        var result = new Tag(tag.TagId, new TagName(tag.Name));
        result.ClearDomainEvents();
        return result;
    }
}
