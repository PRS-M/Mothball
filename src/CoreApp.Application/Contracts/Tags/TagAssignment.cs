namespace CoreApp.Application.Contracts.Tags;

/// <summary>
/// Describes an assignment of a tag to an item or container.
/// </summary>
public sealed record TagAssignment(Guid TagId, Guid TargetId, TagTargetType TargetType);
