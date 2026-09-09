using CoreApp.Domain.Abstractions;

namespace CoreApp.Application.Abstractions.DomainEvents;

/// <summary>
/// Dispatches domain events after a successful application mutation.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches events in the order supplied.</summary>
    /// <param name="domainEvents">The events to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
