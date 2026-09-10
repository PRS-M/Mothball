namespace CoreApp.Domain.Abstractions;

/// <summary>
/// Represents a fact that occurred inside the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Gets the stable identifier used for idempotent synchronization.</summary>
    Guid EventId { get; }

    /// <summary>Gets the time at which the event occurred.</summary>
    DateTimeOffset OccurredUtc { get; }
}
