namespace CoreApp.Specifications;

public sealed record ContainerItemsSpecification(
    string ContainerId,
    string? SearchTerm = null,
    int? PageNumber = null,
    int? PageSize = null);
