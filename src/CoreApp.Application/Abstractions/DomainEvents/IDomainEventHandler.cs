using CoreApp.Domain.Abstractions;

namespace CoreApp.Application.Abstractions.DomainEvents;

/// <summary>
/// Handles one or more domain events after their persistence operation succeeds.
/// </summary>
public interface IDomainEventHandler
{
    /// <summary>Handles the supplied event.</summary>
    /// <param name="domainEvent">The event to handle.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
