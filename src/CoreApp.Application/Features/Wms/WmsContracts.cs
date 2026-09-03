using CoreApp.Domain.Entities.InventoryAggregate;

namespace CoreApp.Application.Features.Wms;

/// <summary>Request to receive integer stock at a warehouse location.</summary>
public sealed record ReceiveStockCommand(Guid WorkspaceId, Guid ItemId, Guid DestinationLocationId, int Quantity, string Reason, Guid? OperationId = null);

/// <summary>Result of a validated receiving operation.</summary>
public sealed record ReceiveStockResult(Guid OperationId, InventoryMovement Movement, InventoryBalance ResultingBalance);

/// <summary>Validates the first experimental WMS receiving vertical slice.</summary>
public static class ReceiveStockValidator
{
    public static void Validate(ReceiveStockCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.WorkspaceId == Guid.Empty) throw new ArgumentException("Workspace ID cannot be empty.", nameof(command));
        if (command.ItemId == Guid.Empty) throw new ArgumentException("Item ID cannot be empty.", nameof(command));
        if (command.DestinationLocationId == Guid.Empty) throw new ArgumentException("Destination location cannot be empty.", nameof(command));
        if (command.Quantity < 1) throw new ArgumentOutOfRangeException(nameof(command.Quantity));
        if (string.IsNullOrWhiteSpace(command.Reason)) throw new ArgumentException("A reason is required.", nameof(command.Reason));
    }
}
