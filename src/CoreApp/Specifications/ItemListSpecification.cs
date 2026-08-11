namespace CoreApp.Specifications;

public enum ItemQueryFilter
{
    All,
    Unassigned,
}

public sealed record ItemListSpecification(
    ItemQueryFilter Filter,
    string? SearchTerm = null,
    int? PageNumber = null,
    int? PageSize = null);
