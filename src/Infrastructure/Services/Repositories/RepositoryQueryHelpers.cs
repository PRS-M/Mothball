using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Repositories;

internal static class RepositoryQueryHelpers
{
    public static void ValidatePaging(int pageNumber, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
    }

    public static bool TryGetPaging(int? pageNumber, int? pageSize, out int pageNumberValue, out int pageSizeValue)
    {
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            pageNumberValue = pageNumber.Value;
            pageSizeValue = pageSize.Value;
            return true;
        }

        pageNumberValue = default;
        pageSizeValue = default;
        return false;
    }

    public static int CalculateOffset(int pageNumber, int pageSize) => pageNumber * pageSize;

    public static (string? term, bool hasSearch) NormalizeSearch(string? searchTerm)
    {
        var term = searchTerm?.Trim();
        return (term, !string.IsNullOrWhiteSpace(term));
    }

    public static bool TryParseGuid(
        string value,
        out Guid result,
        ILogger logger,
        string? methodName = null,
        string? logValue = null)
    {
        if (Guid.TryParse(value, out result))
        {
            return true;
        }

        if (methodName is not null)
        {
            logger.LogWarning("{MethodName}: invalid GUID format: {Value}", methodName, logValue ?? value);
        }

        return false;
    }

    public static Task<List<T>> QueryAllOrderedByRowIdAsync<T>(
        IRepository<T> repository,
        int? pageNumber = null,
        int? pageSize = null) where T : new()
    {
        if (TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            ValidatePaging(pageNumberValue, pageSizeValue);
            int offset = CalculateOffset(pageNumberValue, pageSizeValue);
            return repository.QueryAsync(
                $"SELECT * FROM {typeof(T).Name} ORDER BY rowid LIMIT ? OFFSET ?",
                pageSizeValue,
                offset);
        }

        return repository.QueryAsync($"SELECT * FROM {typeof(T).Name} ORDER BY rowid");
    }

    public static Dictionary<TKey, IEnumerable<T>> GroupByKey<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> keySelector) where TKey : notnull
        => items.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.AsEnumerable());

    public static async Task<Dictionary<TKey, IEnumerable<T>>> LoadLookupByIdsAsync<T, TKey>(
        IRepository<T> repository,
        string propertyName,
        IEnumerable<object> ids,
        Func<T, TKey> keySelector)
        where T : new()
        where TKey : notnull
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        List<T> rows = await repository.WhereInAsync(propertyName, idList);
        return GroupByKey(rows, keySelector);
    }
}
