using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore;

/// <summary>JSON implementation of canonical balance and movement persistence.</summary>
public sealed class JsonCanonicalInventoryRepository(JsonInventoryStore store) : ICanonicalInventoryRepository
{
    public async Task<InventoryBalance?> GetBalanceAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, CancellationToken cancellationToken = default)
        => Map((await store.LoadAsync().ConfigureAwait(false)).CanonicalBalances.FirstOrDefault(x => x.WorkspaceId == workspaceId.Value && x.ItemId == itemId && x.PlacementId == placementId.Value));

    public async Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(InventoryWorkspaceId workspaceId, Guid itemId, CancellationToken cancellationToken = default)
        => (await store.LoadAsync().ConfigureAwait(false)).CanonicalBalances.Where(x => x.WorkspaceId == workspaceId.Value && x.ItemId == itemId).Select(x => Map(x)!).ToList();

    public Task ApplyAsync(InventoryMovementPlan plan, CancellationToken cancellationToken = default)
        => store.UpdateAsync(state =>
        {
            if (state.CanonicalMovements.Any(x => x.MovementId == plan.Movement.MovementId)) return Task.CompletedTask;
            state.CanonicalMovements.Add(new JsonCanonicalMovementRow
            {
                MovementId = plan.Movement.MovementId, WorkspaceId = plan.Movement.WorkspaceId.Value, ItemId = plan.Movement.ItemId,
                Type = (int)plan.Movement.Type, Quantity = plan.Movement.Quantity, SourcePlacementId = plan.Movement.SourcePlacementId?.Value,
                DestinationPlacementId = plan.Movement.DestinationPlacementId?.Value, Reason = plan.Movement.Reason, OccurredUtc = plan.Movement.OccurredUtc,
            });
            foreach (var balance in plan.ResultingBalances)
            {
                var row = state.CanonicalBalances.FirstOrDefault(x => x.WorkspaceId == balance.WorkspaceId.Value && x.ItemId == balance.ItemId && x.PlacementId == balance.PlacementId.Value);
                if (row is null) state.CanonicalBalances.Add(ToRow(balance));
                else { row.OnHandQuantity = balance.OnHandQuantity; row.Version = balance.Version; }
            }
            return Task.CompletedTask;
        });

    private static InventoryBalance? Map(JsonCanonicalBalanceRow? x) => x is null ? null : new(new InventoryWorkspaceId(x.WorkspaceId), x.ItemId, new InventoryPlacementId(x.PlacementId), x.OnHandQuantity, x.Version);
    private static JsonCanonicalBalanceRow ToRow(InventoryBalance x) => new() { WorkspaceId = x.WorkspaceId.Value, ItemId = x.ItemId, PlacementId = x.PlacementId.Value, OnHandQuantity = x.OnHandQuantity, Version = x.Version };
}
