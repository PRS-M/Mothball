using CoreApp.Domain.Entities.InventoryAggregate;

namespace CoreApp.Domain.Inventory;

public static class ItemInventoryConsumptionPlanner
{
    public static ItemInventoryWithdrawalPlan Plan(
        InventorySnapshot inventory,
        ItemInventoryConsumptionSource source,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(source);

        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Consumption quantity must be positive.");
        }

        return source.Kind switch
        {
            ItemInventoryConsumptionSourceKind.Container => PlanContainerConsumption(inventory, source, quantity),
            ItemInventoryConsumptionSourceKind.Unassigned => PlanUnassignedConsumption(inventory, source, quantity),
            _ => throw new ArgumentOutOfRangeException(nameof(source), "Unsupported consumption source."),
        };
    }

    private static ItemInventoryWithdrawalPlan PlanContainerConsumption(
        InventorySnapshot inventory,
        ItemInventoryConsumptionSource source,
        int quantity)
    {
        if (source.ContainerId is null || source.ContainerId == Guid.Empty)
        {
            throw new ArgumentException("Container consumption requires a container ID.", nameof(source));
        }

        var allocation = inventory.Allocations.FirstOrDefault(candidate =>
            candidate.ContainerId == source.ContainerId.Value);
        if (allocation is null || quantity > allocation.Quantity)
        {
            throw new InvalidOperationException("The selected container does not have enough stock.");
        }

        var allocations = inventory.Allocations
            .Select(candidate => candidate.ContainerId == allocation.ContainerId
                ? candidate with { Quantity = candidate.Quantity - quantity }
                : candidate)
            .Where(candidate => candidate.Quantity > 0)
            .ToList();

        return CreatePlan(inventory.TotalQuantity - quantity, allocations);
    }

    private static ItemInventoryWithdrawalPlan PlanUnassignedConsumption(
        InventorySnapshot inventory,
        ItemInventoryConsumptionSource source,
        int quantity)
    {
        if (source.ContainerId is not null)
        {
            throw new ArgumentException("Unassigned consumption cannot specify a container ID.", nameof(source));
        }

        if (quantity > inventory.UnassignedQuantity)
        {
            throw new InvalidOperationException("There is not enough unassigned stock.");
        }

        return CreatePlan(inventory.TotalQuantity - quantity, inventory.Allocations);
    }

    private static ItemInventoryWithdrawalPlan CreatePlan(
        int totalQuantity,
        IReadOnlyList<ItemContainerAllocation> allocations)
    {
        int assignedQuantity = allocations.Sum(allocation => allocation.Quantity);
        int unassignedQuantity = totalQuantity - assignedQuantity;
        return new ItemInventoryWithdrawalPlan(
            totalQuantity,
            assignedQuantity,
            unassignedQuantity,
            allocations,
            DeleteItem: totalQuantity == 0);
    }
}
