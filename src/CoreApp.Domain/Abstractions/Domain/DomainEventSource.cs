namespace CoreApp.Domain.Abstractions;

/// <summary>
/// Provides the common pending-event implementation for aggregate roots.
/// </summary>
public abstract class DomainEventSource : IDomainEventSource
{
    private readonly List<IDomainEvent> domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() => domainEvents.Clear();

    /// <summary>Records an event produced by the aggregate.</summary>
    /// <param name="domainEvent">The event to record.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        domainEvents.Add(domainEvent);
    }
}
