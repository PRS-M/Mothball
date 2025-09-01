using System;

namespace CoreApp.Entities.ContainerAggregate;

public class StoredItem
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public List<Photo> Photos { get; set; } = new List<Photo>();
}
