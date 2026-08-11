namespace CoreApp.Specifications;

public enum ContainerQueryFilter
{
    All,
    Empty,
}

public sealed record ContainerListSpecification(
    ContainerQueryFilter Filter,
    string? SearchTerm = null,
    int? PageNumber = null,
    int? PageSize = null);
