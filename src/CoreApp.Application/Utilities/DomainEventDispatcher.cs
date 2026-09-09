using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Domain.Abstractions;

namespace CoreApp.Application.Utilities;

/// <summary>
/// Dispatches in-process domain events to registered application handlers.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IReadOnlyCollection<IDomainEventHandler> handlers;

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
        }
    }
}
