namespace CoreApp.Application.Contracts.Tags;

/// <summary>
/// Specifies exact tags that must be present when querying one aggregate type.
/// </summary>
public sealed record TagFilter(
    TagTargetType TargetType,
    IReadOnlyCollection<string> Names,
    bool MatchAll = true);
