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

    public static Dictionary<TKey, IEnumerable<T>> GroupByKey<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> keySelector) where TKey : notnull
        => items.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.AsEnumerable());
}
