using CoreApp.Domain.Abstractions;
using CoreApp.Domain.Entities;

namespace CoreApp.Domain.Entities.InventoryAggregate;

public sealed class ItemInventory : BaseEntity, IAggregateRoot
{
    private readonly List<ItemContainerAllocation> allocations = [];

    public ItemInventory(Guid itemId, int totalQuantity = 1)
        : this(itemId, totalQuantity, [])
    {
    }

    public ItemInventory(Guid itemId, int totalQuantity, IEnumerable<ItemContainerAllocation> allocations)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
        }

        ItemId = itemId;
        SetTotalQuantity(totalQuantity);

        foreach (var allocation in allocations)
        {
            SetContainerAllocation(allocation.ContainerId, allocation.ContainerName, allocation.Quantity);
        }
    }

    public Guid ItemId { get; }
    public int TotalQuantity { get; private set; }
    public IReadOnlyList<ItemContainerAllocation> Allocations => allocations.AsReadOnly();
    public int AssignedQuantity => allocations.Sum(allocation => allocation.Quantity);
    public int UnassignedQuantity => TotalQuantity - AssignedQuantity;

    public void IncreaseTotalQuantity(int totalQuantity)
    {
        if (totalQuantity > TotalQuantity)
        {
            TotalQuantity = totalQuantity;
        }
    }

    public void SetTotalQuantity(int totalQuantity)
    {
        if (totalQuantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalQuantity), "Total quantity must be at least one.");
        }

        if (totalQuantity < AssignedQuantity)
        {
            throw new InvalidOperationException("Total quantity cannot be less than assigned quantity.");
        }

        TotalQuantity = totalQuantity;
    }

    public void SetContainerAllocation(Guid containerId, string containerName, int quantity)
    {
        if (containerId == Guid.Empty)
        {
            throw new ArgumentException("Container ID cannot be empty.", nameof(containerId));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Allocated quantity cannot be negative.");
        }

        var existingIndex = allocations.FindIndex(allocation => allocation.ContainerId == containerId);
        var previousQuantity = existingIndex >= 0 ? allocations[existingIndex].Quantity : 0;
        var resultingAssignedQuantity = AssignedQuantity - previousQuantity + quantity;

        IncreaseTotalQuantity(resultingAssignedQuantity);

        if (quantity == 0)
        {
            if (existingIndex >= 0)
            {
                allocations.RemoveAt(existingIndex);
            }

            return;
        }

        var allocation = new ItemContainerAllocation(containerId, containerName, quantity);
        if (existingIndex >= 0)
        {
            allocations[existingIndex] = allocation;
        }
        else
        {
            allocations.Add(allocation);
        }

        allocations.Sort((left, right) =>
            string.Compare(left.ContainerName, right.ContainerName, StringComparison.OrdinalIgnoreCase));
    }

    public void ApplyWithdrawal(ItemInventoryWithdrawalPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.DeleteItem)
        {
            if (plan.TotalQuantity != 0 || plan.AssignedQuantity != 0 || plan.UnassignedQuantity != 0)
            {
                throw new ArgumentException("A deletion plan must exhaust all inventory.", nameof(plan));
            }

            allocations.Clear();
            TotalQuantity = 0;
            return;
        }

        if (plan.TotalQuantity < 1
            || plan.AssignedQuantity < 0
            || plan.UnassignedQuantity != plan.TotalQuantity - plan.AssignedQuantity
            || plan.Allocations.Sum(allocation => allocation.Quantity) != plan.AssignedQuantity)
        {
            throw new ArgumentException("Withdrawal plan quantities are inconsistent.", nameof(plan));
        }

        allocations.Clear();
        TotalQuantity = plan.TotalQuantity;
        foreach (var allocation in plan.Allocations.Where(allocation => allocation.Quantity > 0))
        {
            SetContainerAllocation(allocation.ContainerId, allocation.ContainerName, allocation.Quantity);
        }
    }
}