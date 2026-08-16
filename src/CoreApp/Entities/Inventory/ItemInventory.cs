using CoreApp.Entities;

namespace CoreApp.Entities.Inventory;

public sealed class ItemInventory : BaseEntity, IAggregateRoot
{
    private readonly List<ItemContainerAllocation> allocations = [];

    public ItemInventory(Guid itemId, int totalQuantity = 1)
        : this(itemId, totalQuantity, [])
    {
    }

    public ItemInventory(
        Guid itemId,
        int totalQuantity,
        IEnumerable<ItemContainerAllocation> allocations)
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

    /// <summary>
    /// Increases the total quantity when the supplied value is greater than the current total.
    /// </summary>
    /// <param name="totalQuantity">The proposed total quantity.</param>
    public void IncreaseTotalQuantity(int totalQuantity)
    {
        if (totalQuantity > TotalQuantity)
        {
            TotalQuantity = totalQuantity;
        }
    }

    /// <summary>
    /// Sets the total quantity while preserving all existing allocations.
    /// </summary>
    /// <param name="totalQuantity">The new total quantity, which must be at least one and no less than the assigned quantity.</param>
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

    /// <summary>
    /// Sets the quantity of this item allocated to a container.
    /// </summary>
    /// <param name="containerId">The identifier of the container.</param>
    /// <param name="containerName">The display name of the container.</param>
    /// <param name="quantity">The allocation quantity; zero removes the allocation.</param>
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

        int existingIndex = allocations.FindIndex(allocation => allocation.ContainerId == containerId);
        int previousQuantity = existingIndex < 0 ? 0 : allocations[existingIndex].Quantity;
        int resultingAssignedQuantity = AssignedQuantity - previousQuantity + quantity;

        if (resultingAssignedQuantity > TotalQuantity)
        {
            TotalQuantity = resultingAssignedQuantity;
        }

        if (existingIndex >= 0)
        {
            allocations.RemoveAt(existingIndex);
        }

        if (quantity > 0)
        {
            allocations.Add(new ItemContainerAllocation(containerId, containerName, quantity));
            allocations.Sort((left, right) =>
                string.Compare(left.ContainerName, right.ContainerName, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Applies a validated inventory withdrawal plan.
    /// </summary>
    /// <param name="plan">The withdrawal plan that defines the resulting inventory state.</param>
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
