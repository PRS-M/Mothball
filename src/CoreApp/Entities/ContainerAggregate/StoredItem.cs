using System;

namespace CoreApp.Entities.ContainerAggregate;

public class StoredItem
{
    public StoredItem(Guid itemId, int quantity)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("ItemId cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        ItemId = itemId;
        Quantity = quantity;
    }

    public Guid ItemId { get; }

    public int Quantity { get; private set; }

    public void AddQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        Quantity += quantity;
    }
}
