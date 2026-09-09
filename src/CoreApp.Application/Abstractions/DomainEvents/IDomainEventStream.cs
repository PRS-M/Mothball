using CoreApp.Domain.Abstractions;

namespace CoreApp.Application.Abstractions.DomainEvents;

/// <summary>
/// Provides a subscription point for in-process domain-event notifications.
/// </summary>
public interface IDomainEventStream
{
    /// <summary>Subscribes to events until the returned handle is disposed.</summary>
    /// <param name="subscriber">The callback receiving published events.</param>
    IDisposable Subscribe(Action<IDomainEvent> subscriber);
}
