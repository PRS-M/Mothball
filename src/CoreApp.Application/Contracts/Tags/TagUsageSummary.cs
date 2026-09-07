namespace CoreApp.Application.Contracts.Tags;

/// <summary>
/// Describes a tag and the number of live item and container assignments that use it.
/// </summary>
public sealed record TagUsageSummary(
    Guid TagId,
    string Name,
    int ItemCount,
    int ContainerCount)
{
    /// <summary>
    /// Gets the total number of item and container assignments for the tag.
    /// </summary>
    public int TotalCount => ItemCount + ContainerCount;
}
