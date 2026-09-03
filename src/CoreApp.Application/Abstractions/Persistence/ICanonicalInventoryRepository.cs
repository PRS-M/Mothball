using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;

namespace CoreApp.Application.Abstractions.Persistence;

/// <summary>Persists canonical balances and immutable movement history atomically.</summary>
public interface ICanonicalInventoryRepository
{
    Task<InventoryBalance?> GetBalanceAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(InventoryWorkspaceId workspaceId, Guid itemId, CancellationToken cancellationToken = default);
    Task ApplyAsync(InventoryMovementPlan plan, CancellationToken cancellationToken = default);
}
