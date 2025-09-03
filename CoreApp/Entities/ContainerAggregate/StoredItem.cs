using System;
using CoreApp.Entities.Shared;

namespace CoreApp.Entities.ContainerAggregate;

public class StoredItem
{
    public Guid ItemId { get; set; } = Guid.Empty;
    public int Quantity { get; set; }
}
