using System;

namespace CoreApp.Models;

public class InventoryRoot
{
    public Dictionary<string, Container> Containers { get; set; } = new Dictionary<string, Container>();
    // Items keyed by UniqueId (GUID)
    public Dictionary<string, Item> Items { get; set; } = new Dictionary<string, Item>();
    public Dictionary<string, List<string>> ItemIdsByContainerId { get; set; } = new Dictionary<string, List<string>>();

#pragma warning disable CA1822 // false positive in certain analyzers suggesting static
    public void AddContainer(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (string.IsNullOrEmpty(container.UniqueId))
            throw new ArgumentException("Container must have a unique ID.");

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
        if (string.IsNullOrEmpty(item.UniqueId)) item.UniqueId = Guid.NewGuid().ToString();
        Items[item.UniqueId] = item;
    }

    public void RemoveItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) throw new ArgumentNullException(nameof(itemId));
        Items.Remove(itemId);
        // Also remove from any container mappings
        foreach (var kvp in ItemIdsByContainerId)
        {
            kvp.Value.Remove(itemId);
        }
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

    public string AddNewItemAndAssign(string containerId, Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrEmpty(item.UniqueId)) item.UniqueId = Guid.NewGuid().ToString();
        Items[item.UniqueId] = item;
        AddItemToContainer(containerId, item.UniqueId);
        return item.UniqueId;
    }
#pragma warning restore CA1822
}
