using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.Entities.TagAggregate;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore.Repositories;

/// <summary>
/// JSON operational-store persistence for reusable tags and their assignments.
/// </summary>
public sealed class JsonTagRepository : ITagRepository
{
    private readonly JsonInventoryStore store;

    public JsonTagRepository(JsonInventoryStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tag>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return state.Tags
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToDomain)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagUsageSummary>> GetUsageSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var assignments = state.TagAssignments
            .GroupBy(assignment => assignment.TagId)
            .ToDictionary(
                group => group.Key,
                group => (
                    ItemCount: group.Count(assignment => assignment.TargetType == TagTargetType.Item),
                    ContainerCount: group.Count(assignment => assignment.TargetType == TagTargetType.Container)));

        return state.Tags
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tag =>
            {
                assignments.TryGetValue(tag.TagId, out var counts);
                return new TagUsageSummary(tag.TagId, tag.Name, counts.ItemCount, counts.ContainerCount);
            })
            .ToList();
    }

    public async Task<Tag?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var normalized = new TagName(normalizedName).NormalizedValue;
        var state = await store.LoadAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var row = state.Tags.FirstOrDefault(tag => tag.NormalizedName == normalized);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Tag> GetOrCreateAsync(
        TagName name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        Tag? result = null;

        await store.UpdateAsync(state =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = state.Tags.FirstOrDefault(tag => tag.NormalizedName == name.NormalizedValue);
            if (existing is not null)
            {
                result = ToDomain(existing);
                return Task.CompletedTask;
            }

            var row = new JsonTagRow
            {
                TagId = Guid.NewGuid(),
                Name = name.Value,
                NormalizedName = name.NormalizedValue,
            };
            state.Tags.Add(row);
            result = ToDomain(row);

            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Tag creation did not produce a result.");
    }

    public async Task<IReadOnlyList<Tag>> GetForTargetAsync(
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        ValidateTarget(targetId);
        var state = await store.LoadAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var tagIds = state.TagAssignments
            .Where(assignment => assignment.TargetType == targetType && assignment.TargetId == targetId)
            .Select(assignment => assignment.TagId)
            .ToHashSet();
        return state.Tags
            .Where(tag => tagIds.Contains(tag.TagId))
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToDomain)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetTargetIdsAsync(
        Guid tagId,
        TagTargetType targetType,
        CancellationToken cancellationToken = default)
    {
        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("Tag ID cannot be empty.", nameof(tagId));
        }

        var state = await store.LoadAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return state.TagAssignments
            .Where(assignment => assignment.TagId == tagId && assignment.TargetType == targetType)
            .Select(assignment => assignment.TargetId)
            .ToHashSet();
    }

    public Task AssignAsync(
        Guid tagId,
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(tagId, targetId);

        return store.UpdateAsync(state =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (state.TagAssignments.Any(assignment =>
                    assignment.TagId == tagId
                    && assignment.TargetType == targetType
                    && assignment.TargetId == targetId))
            {
                return Task.CompletedTask;
            }

            state.TagAssignments.Add(new JsonTagAssignmentRow
            {
                Id = state.Metadata.NextTagAssignmentId++,
                TagId = tagId,
                TargetId = targetId,
                TargetType = targetType,
            });

            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task RemoveAsync(
        Guid tagId,
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(tagId, targetId);

        return store.UpdateAsync(state =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.TagAssignments.RemoveAll(assignment =>
                assignment.TagId == tagId
                && assignment.TargetType == targetType
                && assignment.TargetId == targetId);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    private static Tag ToDomain(JsonTagRow row)
        => new(row.TagId, new TagName(row.Name));

    private static void ValidateTarget(Guid targetId)
    {
        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("Tag target ID cannot be empty.", nameof(targetId));
        }
    }

    private static void ValidateIds(Guid tagId, Guid targetId)
    {
        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("Tag ID cannot be empty.", nameof(tagId));
        }

        ValidateTarget(targetId);
    }
}
