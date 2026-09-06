using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.Entities.TagAggregate;
using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Abstractions.Persistence;

/// <summary>
/// Defines persistence operations for reusable tags and their aggregate assignments.
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Finds a tag by its normalized name.
    /// </summary>
    Task<Tag?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tag, creating it when no tag with the same normalized name exists.
    /// </summary>
    Task<Tag> GetOrCreateAsync(
        TagName name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tags assigned to an item or container.
    /// </summary>
    Task<IReadOnlyList<Tag>> GetForTargetAsync(
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a tag idempotently to an item or container.
    /// </summary>
    Task AssignAsync(
        Guid tagId,
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a tag assignment when it exists.
    /// </summary>
    Task RemoveAsync(
        Guid tagId,
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);
}
