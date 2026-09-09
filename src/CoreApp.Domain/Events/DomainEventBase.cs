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
        OccurredUtc = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public DateTimeOffset OccurredUtc { get; init; }
}
