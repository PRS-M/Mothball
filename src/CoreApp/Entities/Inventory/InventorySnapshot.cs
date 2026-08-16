using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Entities.Inventory;

public sealed record InventorySnapshot
{
    public InventorySnapshot(
        Item item,
        int totalQuantity,
        int assignedQuantity,
        IReadOnlyList<ItemContainerAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(allocations);

        if (totalQuantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalQuantity));
        }

        if (assignedQuantity < 0 || assignedQuantity > totalQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(assignedQuantity));
        }

        if (allocations.Sum(allocation => allocation.Quantity) != assignedQuantity)
        {
            throw new ArgumentException("Allocations must sum to assigned quantity.", nameof(allocations));
        }

        Item = item;
        TotalQuantity = totalQuantity;
        AssignedQuantity = assignedQuantity;
        Allocations = allocations;
    }

    public Item Item { get; }
    public int TotalQuantity { get; }
    public int AssignedQuantity { get; }
    public IReadOnlyList<ItemContainerAllocation> Allocations { get; }
    public int UnassignedQuantity => TotalQuantity - AssignedQuantity;
}

public sealed record ContainerItemInventoryEntry(
    InventorySnapshot Inventory,
    int ContainerQuantity);
