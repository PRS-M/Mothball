using CoreApp.Domain.Entities.InventoryAggregate;

namespace CoreApp.Application.Features.Wms;

/// <summary>Executes the first experimental WMS receiving workflow.</summary>
public sealed class ReceiveStockHandler(CanonicalInventoryCommandService inventory)
{
    /// <summary>Receives stock at a location using the canonical movement path.</summary>
    public async Task<ReceiveStockResult> HandleAsync(ReceiveStockCommand command, CancellationToken cancellationToken = default)
    {
        ReceiveStockValidator.Validate(command);
        var operationId = command.OperationId ?? Guid.NewGuid();
        var plan = await inventory.ReceiveAsync(new InventoryWorkspaceId(command.WorkspaceId), command.ItemId, new InventoryPlacementId(command.DestinationLocationId), command.Quantity, command.Reason, operationId, cancellationToken).ConfigureAwait(false);
        return new ReceiveStockResult(operationId, plan.Movement, plan.ResultingBalances.Single());
    }
}
