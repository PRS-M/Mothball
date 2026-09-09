using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Domain.Abstractions;

namespace CoreApp.Application.Utilities;

/// <summary>
/// Preserves the existing list-cache invalidation behavior through domain events.
/// </summary>
public sealed class InventoryRevisionEventHandler : IDomainEventHandler
{
    private readonly IInventoryChangeTracker inventoryChanges;

    public InventoryRevisionEventHandler(IInventoryChangeTracker inventoryChanges)
    {
        this.inventoryChanges = inventoryChanges
            ?? throw new ArgumentNullException(nameof(inventoryChanges));
    }

    /// <inheritdoc />
    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        cancellationToken.ThrowIfCancellationRequested();
        inventoryChanges.MarkChanged();
        return Task.CompletedTask;
    }
}
