namespace CoreApp.Specifications;

public enum ItemQueryFilter
{
    All,
    Unassigned,
}

/// <summary>
/// Defines item list query semantics shared by all persistence backends.
/// All-item queries and item search results are ordered by insertion order.
/// Unassigned item queries are ordered by name case-insensitively.
/// </summary>
public sealed record ItemListSpecification(
    ItemQueryFilter Filter,
    string? SearchTerm = null,
    int? PageNumber = null,
    int? PageSize = null,
    Guid? ExcludedContainerId = null);
