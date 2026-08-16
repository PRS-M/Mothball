using CoreApp.Entities.Inventory;
using CoreApp.Contracts;

namespace CoreApp.Features.Inventory.Withdrawal;

public static class ItemInventoryWithdrawalPlanner
{
    public static ItemContainerAllocation? GetPreferredAllocation(
        IReadOnlyCollection<ItemContainerAllocation> allocations,
        Guid? sourceContainerId)
    {
        ArgumentNullException.ThrowIfNull(allocations);

        if (sourceContainerId is null || sourceContainerId == Guid.Empty)
        {
            return null;
        }

        return allocations.FirstOrDefault(allocation =>
            allocation.ContainerId == sourceContainerId && allocation.Quantity > 0);
    }

    public static int GetRequiredAssignedWithdrawal(
        int currentTotal,
        int requestedTotal,
        int assignedQuantity)
    {
        if (currentTotal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentTotal));
        }

        if (requestedTotal < 0 || requestedTotal >= currentTotal)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedTotal));
        }

        if (assignedQuantity < 0 || assignedQuantity > currentTotal)
        {
            throw new ArgumentOutOfRangeException(nameof(assignedQuantity));
        }

        return Math.Min(currentTotal - requestedTotal, assignedQuantity);
    }

    public static ItemInventoryWithdrawalPlan Plan(
        int currentTotal,
        IReadOnlyCollection<ItemContainerAllocation> allocations,
        IReadOnlyCollection<ItemAllocationWithdrawal> assignedWithdrawals,
        IReadOnlyCollection<int> unassignedWithdrawals,
        int? requestedTotal = null)
    {
        if (currentTotal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentTotal));
        }

        ArgumentNullException.ThrowIfNull(allocations);
        ArgumentNullException.ThrowIfNull(assignedWithdrawals);
        ArgumentNullException.ThrowIfNull(unassignedWithdrawals);

        int targetTotal = requestedTotal ?? currentTotal;
        if (targetTotal < 0 || targetTotal > currentTotal)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedTotal));
        }

        var remainingAllocations = allocations
            .Select(allocation =>
            {
                if (allocation.ContainerId == Guid.Empty || allocation.Quantity <= 0)
                {
                    throw new ArgumentException("Allocations require a container and positive quantity.", nameof(allocations));
                }

                return allocation;
            })
            .ToList();

        int carriedWithdrawal = 0;
        foreach (var withdrawal in assignedWithdrawals)
        {
            if (withdrawal.Quantity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(assignedWithdrawals));
            }

            if (withdrawal.Quantity == 0)
            {
                break;
            }

            if (withdrawal.Quantity < carriedWithdrawal)
            {
                throw new InvalidOperationException(
                    $"The next withdrawal must be at least the carried amount of {carriedWithdrawal}.");
            }

            int selectedIndex = remainingAllocations.FindIndex(
                allocation => allocation.ContainerId == withdrawal.ContainerId);
            if (selectedIndex < 0)
            {
                throw new ArgumentException("Withdrawal container has no remaining allocation.", nameof(assignedWithdrawals));
            }

            var selectedAllocation = remainingAllocations[selectedIndex];
            int removed = Math.Min(selectedAllocation.Quantity, withdrawal.Quantity);
            remainingAllocations[selectedIndex] = selectedAllocation with
            {
                Quantity = selectedAllocation.Quantity - removed,
            };
            carriedWithdrawal = withdrawal.Quantity - removed;
        }

        if (carriedWithdrawal > 0)
        {
            throw new InvalidOperationException(
                $"A remaining withdrawal of {carriedWithdrawal} must be assigned to another container.");
        }

        int assignedQuantity = remainingAllocations.Sum(allocation => allocation.Quantity);
        if (assignedQuantity > targetTotal)
        {
            throw new InvalidOperationException("Assigned withdrawals do not reduce inventory to the requested total.");
        }

        int totalQuantity = targetTotal;
        int unassignedQuantity = totalQuantity - assignedQuantity;
        foreach (int requestedWithdrawal in unassignedWithdrawals)
        {
            if (requestedWithdrawal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unassignedWithdrawals));
            }

            if (requestedWithdrawal == 0)
            {
                break;
            }

            int removed = Math.Min(requestedWithdrawal, unassignedQuantity);
            totalQuantity -= removed;
            unassignedQuantity -= removed;

            if (totalQuantity == 0)
            {
                break;
            }
        }

        return new ItemInventoryWithdrawalPlan(
            totalQuantity,
            assignedQuantity,
            unassignedQuantity,
            remainingAllocations.AsReadOnly(),
            DeleteItem: totalQuantity == 0);
    }
}