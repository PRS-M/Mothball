using CoreApp.Domain.Abstractions;
using CoreApp.Domain.Events;

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
        SetTotalQuantityWithoutEvent(totalQuantity);

        foreach (var allocation in allocations)
        {
            SetContainerAllocationWithoutEvent(allocation.ContainerId, allocation.ContainerName, allocation.Quantity);
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
            var previousTotalQuantity = TotalQuantity;
            TotalQuantity = totalQuantity;
            AddInventoryChanged(previousTotalQuantity, previousAssignedQuantity: AssignedQuantity, []);
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

        if (TotalQuantity == totalQuantity)
        {
            return;
        }

        var previousTotalQuantity = TotalQuantity;
        var previousAssignedQuantity = AssignedQuantity;
        TotalQuantity = totalQuantity;
        AddInventoryChanged(previousTotalQuantity, previousAssignedQuantity, []);
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
        var previousTotalQuantity = TotalQuantity;
        var previousAssignedQuantity = AssignedQuantity;
        var resultingAssignedQuantity = AssignedQuantity - previousQuantity + quantity;

        if (resultingAssignedQuantity > TotalQuantity)
        {
            TotalQuantity = resultingAssignedQuantity;
        }

        if (quantity == 0)
        {
            if (existingIndex >= 0)
            {
                allocations.RemoveAt(existingIndex);
            }

            if (previousQuantity != 0)
            {
                AddInventoryChanged(
                    previousTotalQuantity,
                    previousAssignedQuantity,
                    [containerId]);
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

        if (previousQuantity != quantity)
        {
            AddInventoryChanged(previousTotalQuantity, previousAssignedQuantity, [containerId]);
        }
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
            var previousTotalQuantity = TotalQuantity;
            TotalQuantity = 0;
            AddDomainEvent(new InventoryWithdrawn(ItemId, previousTotalQuantity, 0, []));
            AddDomainEvent(new ItemExhausted(ItemId));
            return;
        }

        if (plan.TotalQuantity < 1
            || plan.AssignedQuantity < 0
            || plan.UnassignedQuantity != plan.TotalQuantity - plan.AssignedQuantity
            || plan.Allocations.Sum(allocation => allocation.Quantity) != plan.AssignedQuantity)
        {
            throw new ArgumentException("Withdrawal plan quantities are inconsistent.", nameof(plan));
        }

        var previousTotal = TotalQuantity;
        var previousAssigned = AssignedQuantity;
        var affectedContainerIds = allocations
            .Select(allocation => allocation.ContainerId)
            .ToArray();
        allocations.Clear();
        TotalQuantity = plan.TotalQuantity;
        foreach (var allocation in plan.Allocations.Where(allocation => allocation.Quantity > 0))
        {
            SetContainerAllocationWithoutEvent(allocation.ContainerId, allocation.ContainerName, allocation.Quantity);
        }

        AddInventoryChanged(previousTotal, previousAssigned, affectedContainerIds);
        AddDomainEvent(new InventoryWithdrawn(ItemId, previousTotal, TotalQuantity, Allocations.ToArray()));
    }

    private void AddInventoryChanged(
        int previousTotalQuantity,
        int previousAssignedQuantity,
        IReadOnlyCollection<Guid> affectedContainerIds)
        => AddDomainEvent(new InventoryChanged(
            ItemId,
            previousTotalQuantity,
            TotalQuantity,
            previousAssignedQuantity,
            AssignedQuantity,
            affectedContainerIds));

    private void SetTotalQuantityWithoutEvent(int totalQuantity)
    {
        if (totalQuantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalQuantity), "Total quantity must be at least one.");
        }

        TotalQuantity = totalQuantity;
    }

    private void SetContainerAllocationWithoutEvent(Guid containerId, string containerName, int quantity)
    {
        if (containerId == Guid.Empty)
        {
            throw new ArgumentException("Container ID cannot be empty.", nameof(containerId));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Allocated quantity cannot be negative.", nameof(quantity));
        }

        if (quantity == 0)
        {
            return;
        }

        allocations.Add(new ItemContainerAllocation(containerId, containerName, quantity));
        allocations.Sort((left, right) =>
            string.Compare(left.ContainerName, right.ContainerName, StringComparison.OrdinalIgnoreCase));
    }
}
