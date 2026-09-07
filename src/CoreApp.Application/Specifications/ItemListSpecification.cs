using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Specifications;

public enum ItemQueryFilter
{
    All,
    Unassigned,
    Assigned,
}

/// <summary>
/// Defines item list query semantics shared by all persistence backends.
/// All-item queries and item search results are ordered by insertion order.
/// Assigned and unassigned item queries are ordered by name case-insensitively.
/// Tag criteria use exact normalized names.
/// </summary>
public sealed record ItemListSpecification(
    ItemQueryFilter Filter,
    string? SearchTerm = null,
    int? PageNumber = null,
    int? PageSize = null,
    Guid? ExcludedContainerId = null,
    TagFilter? TagCriteria = null);
