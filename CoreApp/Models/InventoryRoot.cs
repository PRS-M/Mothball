using System;

namespace CoreApp.Models;

public class InventoryRoot
{
    public Dictionary<string, Container> Containers { get; set; } = new Dictionary<string, Container>();
    public Dictionary<string, Item> Items { get; set; } = new Dictionary<string, Item>();
    public Dictionary<string, List<string>> ItemIdsByContainerId { get; set; } = new Dictionary<string, List<string>>();


    public void AddContainer(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (string.IsNullOrEmpty(container.UniqueId)) throw new ArgumentException("Container must have a unique ID.");

        Containers[container.UniqueId] = container;
    }

    public void RemoveContainer(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) throw new ArgumentNullException(nameof(uniqueId));
        Containers.Remove(uniqueId);
    }

    public void AddItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrEmpty(item.Name)) throw new ArgumentException("Item must have a name.");

        Items[item.Name] = item;
    }

    public void RemoveItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) throw new ArgumentNullException(nameof(itemName));
        Items.Remove(itemName);
    }

    public void AddItemToContainer(string containerId, string itemId)
    {
        if (!ItemIdsByContainerId.ContainsKey(containerId))
        {
            ItemIdsByContainerId[containerId] = new List<string>();
        }

        ItemIdsByContainerId[containerId].Add(itemId);
    }

    public void RemoveItemFromContainer(string containerId, string itemId)
    {
        if (ItemIdsByContainerId.ContainsKey(containerId))
        {
            ItemIdsByContainerId[containerId].Remove(itemId);
        }
    }
}
