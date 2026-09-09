using CoreApp.Domain.Abstractions;
using CoreApp.Domain.Events;
using CoreApp.Domain.ValueObjects;

namespace CoreApp.Domain.Entities.TagAggregate;

/// <summary>
/// Represents a reusable tag that can be assigned to inventory aggregates.
/// </summary>
public sealed class Tag : BaseEntity, IAggregateRoot
{
    public Tag(Guid tagId, TagName name)
    {
        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("Tag ID cannot be empty.", nameof(tagId));
        }

        TagId = tagId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public Tag(Guid tagId, string name)
        : this(tagId, new TagName(name))
    {
    }

    public Guid TagId { get; }

    /// <summary>
    /// Gets the current tag name and its normalized lookup form.
    /// </summary>
    public TagName Name { get; private set; } = null!;

    /// <summary>
    /// Renames the tag while preserving the value-object invariants.
    /// </summary>
    /// <param name="name">The new tag name.</param>
    public void Rename(TagName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name?.NormalizedValue == name.NormalizedValue)
        {
            return;
        }

        var previousName = Name?.Value ?? string.Empty;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        AddDomainEvent(new TagRenamed(TagId, previousName, Name.Value));
    }
}
