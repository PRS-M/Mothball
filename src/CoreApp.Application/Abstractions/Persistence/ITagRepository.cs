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
    /// Gets all persisted tags ordered by display name.
    /// </summary>
    Task<IReadOnlyList<Tag>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tags with counts of their item and container assignments.
    /// </summary>
    Task<IReadOnlyList<TagUsageSummary>> GetUsageSummariesAsync(
        CancellationToken cancellationToken = default);

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
    /// Gets the item or container identifiers assigned to a tag.
    /// </summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="targetType">The kind of target to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IReadOnlySet<Guid>> GetTargetIdsAsync(
        Guid tagId,
        TagTargetType targetType,
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
