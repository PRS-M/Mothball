using CoreApp.Domain.Abstractions;

namespace CoreApp.Domain.Events;

/// <summary>
/// Supplies common metadata for immutable domain events.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <summary>Initializes a new domain event.</summary>
    protected DomainEventBase()
    {
        EventId = Guid.NewGuid();
        OccurredUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the stable identifier of this event occurrence.</summary>
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredUtc { get; init; }
}
