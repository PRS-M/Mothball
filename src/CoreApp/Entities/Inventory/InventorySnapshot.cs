using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Entities.Inventory;

public sealed record InventorySnapshot
{
    public InventorySnapshot(
        Item item,
        int assignedQuantity,
        IReadOnlyList<ItemContainerAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(allocations);

        if (assignedQuantity < 0 || assignedQuantity > item.TotalQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(assignedQuantity));
        }

        if (allocations.Sum(allocation => allocation.Quantity) != assignedQuantity)
        {
            throw new ArgumentException("Allocations must sum to assigned quantity.", nameof(allocations));
        }

        Item = item;
        AssignedQuantity = assignedQuantity;
        Allocations = allocations;
    }

    public Item Item { get; }
    public int AssignedQuantity { get; }
    public IReadOnlyList<ItemContainerAllocation> Allocations { get; }
    public int TotalQuantity => Item.TotalQuantity;
    public int UnassignedQuantity => TotalQuantity - AssignedQuantity;
}

public sealed record ContainerItemInventoryEntry(
    InventorySnapshot Inventory,
    int ContainerQuantity);
