using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;
using CoreApp.Application.Features.Sync;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Repositories;

/// <summary>SQLite implementation of canonical balance and movement persistence.</summary>
public sealed class SqliteCanonicalInventoryRepository(MothballDatabase database) : ICanonicalInventoryMutationStore
{
    public async Task<InventoryBalance?> GetBalanceAsync(InventoryWorkspaceId workspaceId, Guid itemId, InventoryPlacementId placementId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        var id = BalanceId(workspaceId, itemId, placementId);
        var row = await database.Connection.FindAsync<DbInventoryBalance>(id).ConfigureAwait(false);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(InventoryWorkspaceId workspaceId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        var rows = await database.Connection.Table<DbInventoryBalance>().Where(x => x.WorkspaceId == workspaceId.Value && x.ItemId == itemId).ToListAsync().ConfigureAwait(false);
        return rows.Select(Map).ToList();
    }

    public async Task ApplyAsync(InventoryMovementPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        await database.InitializeAsync().ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            connection.InsertOrReplace(new DbInventoryMovement
            {
                MovementId = plan.Movement.MovementId, WorkspaceId = plan.Movement.WorkspaceId.Value, ItemId = plan.Movement.ItemId,
                Type = (int)plan.Movement.Type, Quantity = plan.Movement.Quantity, SourcePlacementId = plan.Movement.SourcePlacementId?.Value,
                DestinationPlacementId = plan.Movement.DestinationPlacementId?.Value, Reason = plan.Movement.Reason, OccurredUtc = plan.Movement.OccurredUtc,
            });
            foreach (var balance in plan.ResultingBalances)
                connection.InsertOrReplace(ToRow(balance));
        }).ConfigureAwait(false);
    }

    public async Task ApplyWithOutboxAsync(InventoryMovementPlan plan, PendingSyncOperation operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(operation);
        await database.InitializeAsync().ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            if (connection.Find<DbInventoryMovement>(plan.Movement.MovementId) is not null) return;
            connection.Insert(new DbInventoryMovement
            {
                MovementId = plan.Movement.MovementId, WorkspaceId = plan.Movement.WorkspaceId.Value, ItemId = plan.Movement.ItemId,
                Type = (int)plan.Movement.Type, Quantity = plan.Movement.Quantity, SourcePlacementId = plan.Movement.SourcePlacementId?.Value,
                DestinationPlacementId = plan.Movement.DestinationPlacementId?.Value, Reason = plan.Movement.Reason, OccurredUtc = plan.Movement.OccurredUtc,
            });
            foreach (var balance in plan.ResultingBalances) connection.InsertOrReplace(ToRow(balance));
            connection.InsertOrReplace(ToOutboxRow(operation));
        }).ConfigureAwait(false);
    }

    private static string BalanceId(InventoryWorkspaceId workspace, Guid item, InventoryPlacementId placement) => $"{workspace.Value:N}:{item:N}:{placement.Value:N}";
    private static DbInventoryBalance ToRow(InventoryBalance x) => new() { BalanceId = BalanceId(x.WorkspaceId, x.ItemId, x.PlacementId), WorkspaceId = x.WorkspaceId.Value, ItemId = x.ItemId, PlacementId = x.PlacementId.Value, OnHandQuantity = x.OnHandQuantity, Version = x.Version };
    private static InventoryBalance Map(DbInventoryBalance x) => new(new InventoryWorkspaceId(x.WorkspaceId), x.ItemId, new InventoryPlacementId(x.PlacementId), x.OnHandQuantity, x.Version);
    private static DbPendingSyncOperation ToOutboxRow(PendingSyncOperation x) => new() { OperationId = x.OperationId, WorkspaceId = x.WorkspaceId, DeviceId = x.DeviceId, AggregateType = x.AggregateType, AggregateId = x.AggregateId, OperationType = x.OperationType, PayloadVersion = x.PayloadVersion, Payload = x.Payload, BaseServerVersion = x.BaseServerVersion, CreatedUtc = x.CreatedUtc, AttemptCount = x.AttemptCount, LastAttemptUtc = x.LastAttemptUtc, LastErrorCode = x.LastErrorCode, State = (int)x.State };
}
