using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Domain.Abstractions;

namespace CoreApp.Application.Utilities;

/// <summary>
/// Dispatches in-process domain events to registered application handlers.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
    , IDomainEventStream
{
    private readonly IReadOnlyCollection<IDomainEventHandler> handlers;
    private readonly List<Action<IDomainEvent>> subscribers = [];
    private readonly object subscriberGate = new();

    public DomainEventDispatcher(IEnumerable<IDomainEventHandler> handlers)
    {
        this.handlers = handlers?.ToArray()
            ?? throw new ArgumentNullException(nameof(handlers));
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }

            Action<IDomainEvent>[] callbacks;
            lock (subscriberGate)
            {
                callbacks = subscribers.ToArray();
            }

            foreach (var callback in callbacks)
            {
                callback(domainEvent);
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(Action<IDomainEvent> subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        lock (subscriberGate)
        {
            subscribers.Add(subscriber);
        }

        return new Subscription(this, subscriber);
    }

    private void Unsubscribe(Action<IDomainEvent> subscriber)
    {
        lock (subscriberGate)
        {
            subscribers.Remove(subscriber);
        }
    }

    private sealed class Subscription(DomainEventDispatcher owner, Action<IDomainEvent> subscriber) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                owner.Unsubscribe(subscriber);
            }
        }
    }
}
