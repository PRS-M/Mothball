namespace CoreApp.Domain.Abstractions;

/// <summary>
/// Exposes domain events recorded by an aggregate during a mutation.
/// </summary>
public interface IDomainEventSource
{
    /// <summary>Gets the events waiting to be dispatched.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Removes all events that have been captured from the source.</summary>
    void ClearDomainEvents();
}
