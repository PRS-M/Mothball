using CoreApp.Application.Features.Sync;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;

namespace CoreApp.Application.Abstractions.Persistence;

/// <summary>Commits a canonical movement, resulting balances, and its outbox operation atomically.</summary>
public interface ICanonicalInventoryMutationStore : ICanonicalInventoryRepository
{
    Task ApplyWithOutboxAsync(InventoryMovementPlan plan, PendingSyncOperation operation, CancellationToken cancellationToken = default);
}
