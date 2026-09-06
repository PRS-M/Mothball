using CoreApp.Application.Contracts.Tags;

namespace CoreApp.Application.Specifications;

/// <summary>
/// Defines container-item query semantics shared by all persistence backends.
/// Results are ordered by relation insertion order. Search is relation-row based,
/// so duplicate relations intentionally produce duplicate item rows. Tag criteria
/// use exact normalized names.
/// </summary>
public sealed record ContainerItemsSpecification(
    string ContainerId,
    string? SearchTerm = null,
    int? PageNumber = null,
    int? PageSize = null,
    TagFilter? TagCriteria = null);
