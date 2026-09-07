using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Contracts.Tags;

/// <summary>
/// Specifies exact tags that must be present when querying one aggregate type.
/// </summary>
public sealed record TagFilter(
    TagTargetType TargetType,
    IReadOnlyCollection<string> Names,
    bool MatchAll = true,
    Guid? TagId = null)
{
    /// <summary>
    /// Gets distinct normalized names for exact backend filtering.
    /// </summary>
    public IReadOnlyList<string> NormalizedNames
        => Names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => new TagName(name).NormalizedValue)
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
