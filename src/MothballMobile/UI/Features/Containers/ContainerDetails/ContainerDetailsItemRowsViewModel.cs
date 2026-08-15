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

    public void Reset()
    {
        items.Clear();
        rows.Clear();
        rows.Add(header);
    }

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

    public ItemWithPhotosViewModel? Find(Guid itemId)
        => items.FirstOrDefault(x => x.Item.ItemId == itemId);

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
