namespace CoreApp.Application.Contracts.Tags;

/// <summary>
/// Describes a persisted tag using its stable identifier and display name.
/// </summary>
public sealed record TagDescriptor(Guid TagId, string Name);
