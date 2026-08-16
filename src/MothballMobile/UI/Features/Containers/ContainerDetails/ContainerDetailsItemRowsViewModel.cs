using CoreApp.Entities.Inventory;
using System.Collections.ObjectModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Contracts;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

internal sealed class ContainerDetailsItemRowsViewModel
{
    private readonly object header;
    private readonly ObservableCollection<ItemWithPhotosViewModel> items;
    private readonly ObservableCollection<object> rows;

    public ContainerDetailsItemRowsViewModel(
        object header,
        ObservableCollection<ItemWithPhotosViewModel> items,
        ObservableCollection<object> rows)
    {
        this.header = header ?? throw new ArgumentNullException(nameof(header));
        this.items = items ?? throw new ArgumentNullException(nameof(items));
        this.rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public bool IsEmpty => items.Count == 0;

    /// <summary>
    /// Clears all item rows and restores the header row.
    /// </summary>
    public void Reset()
    {
        items.Clear();
        rows.Clear();
        rows.Add(header);
    }

    /// <summary>
    /// Clears all item rows while preserving the header row.
    /// </summary>
    public void ClearItems()
    {
        items.Clear();

        if (rows.Count == 0)
        {
            rows.Add(header);
            return;
        }

        for (var index = rows.Count - 1; index > 0; index--)
        {
            rows.RemoveAt(index);
        }
    }

    /// <summary>
    /// Creates and adds view models for container item entries.
    /// </summary>
    /// <param name="sourceItems">The container item entries to add.</param>
    /// <param name="createViewModel">Creates a view model for each entry.</param>
    public void Append(
        IEnumerable<ContainerItemInventoryEntry> sourceItems,
        Func<ContainerItemInventoryEntry, ItemWithPhotosViewModel> createViewModel)
    {
        ArgumentNullException.ThrowIfNull(sourceItems);
        ArgumentNullException.ThrowIfNull(createViewModel);

        foreach (var item in sourceItems)
        {
            var itemViewModel = createViewModel(item);
            items.Add(itemViewModel);
            rows.Add(itemViewModel);
        }
    }

    /// <summary>
    /// Finds the view model for an item.
    /// </summary>
    /// <param name="itemId">The identifier of the item to find.</param>
    /// <returns>The matching view model, or <see langword="null"/> when it is not present.</returns>
    public ItemWithPhotosViewModel? Find(Guid itemId)
        => items.FirstOrDefault(x => x.Item.ItemId == itemId);

    /// <summary>
    /// Removes an item's view model from the item and row collections.
    /// </summary>
    /// <param name="itemId">The identifier of the item to remove.</param>
    /// <returns><see langword="true"/> when an item was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(Guid itemId)
    {
        var itemViewModel = Find(itemId);
        if (itemViewModel is null)
        {
            return false;
        }

        items.Remove(itemViewModel);
        rows.Remove(itemViewModel);
        return true;
    }
}
