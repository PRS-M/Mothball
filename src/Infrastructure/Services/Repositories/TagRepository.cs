using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.Entities.TagAggregate;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.Database;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// SQLite persistence for reusable tags and their item/container assignments.
/// </summary>
public sealed class TagRepository : ITagRepository
{
    private readonly MothballDatabase database;

    public TagRepository(MothballDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tag>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await database.Connection
            .Table<DbTag>()
            .OrderBy(tag => tag.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<Tag?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var normalized = new TagName(normalizedName).NormalizedValue;
        await database.InitializeAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var row = await database.Connection
            .Table<DbTag>()
            .Where(tag => tag.NormalizedName == normalized)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return row?.ToDomain();
    }

    public async Task<Tag> GetOrCreateAsync(
        TagName name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        var existing = await FindByNormalizedNameAsync(name.NormalizedValue, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var tag = new Tag(Guid.NewGuid(), name);
        await database.Connection.InsertAsync(tag.ToDb()).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return tag;
    }

    public async Task<IReadOnlyList<Tag>> GetForTargetAsync(
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        ValidateTarget(targetId);
        await database.InitializeAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        List<DbTag> rows = targetType switch
        {
            TagTargetType.Item => await database.Connection.QueryAsync<DbTag>(
                $"SELECT t.* FROM {nameof(DbTag)} t INNER JOIN {nameof(DbItemTag)} a ON a.TagId = t.TagId WHERE a.ItemId = ? ORDER BY t.Name COLLATE NOCASE",
                targetId).ConfigureAwait(false),
            TagTargetType.Container => await database.Connection.QueryAsync<DbTag>(
                $"SELECT t.* FROM {nameof(DbTag)} t INNER JOIN {nameof(DbContainerTag)} a ON a.TagId = t.TagId WHERE a.ContainerId = ? ORDER BY t.Name COLLATE NOCASE",
                targetId).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Unsupported tag target type '{targetType}'."),
        };

        cancellationToken.ThrowIfCancellationRequested();
        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task AssignAsync(
        Guid tagId,
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(tagId, targetId);
        await database.InitializeAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        string table = targetType switch
        {
            TagTargetType.Item => nameof(DbItemTag),
            TagTargetType.Container => nameof(DbContainerTag),
            _ => throw new NotSupportedException($"Unsupported tag target type '{targetType}'."),
        };
        string targetColumn = targetType == TagTargetType.Item ? nameof(DbItemTag.ItemId) : nameof(DbContainerTag.ContainerId);

        await database.Connection.ExecuteAsync(
            $"INSERT OR IGNORE INTO {table} ({targetColumn}, TagId) VALUES (?, ?)",
            targetId,
            tagId).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task RemoveAsync(
        Guid tagId,
        TagTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(tagId, targetId);
        await database.InitializeAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        string table = targetType switch
        {
            TagTargetType.Item => nameof(DbItemTag),
            TagTargetType.Container => nameof(DbContainerTag),
            _ => throw new NotSupportedException($"Unsupported tag target type '{targetType}'."),
        };
        string targetColumn = targetType == TagTargetType.Item ? nameof(DbItemTag.ItemId) : nameof(DbContainerTag.ContainerId);

        await database.Connection.ExecuteAsync(
            $"DELETE FROM {table} WHERE {targetColumn} = ? AND TagId = ?",
            targetId,
            tagId).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

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

        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("Tag target ID cannot be empty.", nameof(targetId));
        }
    }
}
