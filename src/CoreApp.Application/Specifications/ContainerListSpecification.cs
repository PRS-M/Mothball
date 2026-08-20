namespace CoreApp.Specifications;

public enum ContainerQueryFilter
{
    All,
    Empty,
}

/// <summary>
/// Defines container list query semantics shared by all persistence backends.
/// All-container queries are ordered by insertion order. Search and empty-container
/// queries are ordered by name case-insensitively.
/// </summary>
public sealed record ContainerListSpecification(
    ContainerQueryFilter Filter,
    string? SearchTerm = null,
    int? PageNumber = null,
    int? PageSize = null);
