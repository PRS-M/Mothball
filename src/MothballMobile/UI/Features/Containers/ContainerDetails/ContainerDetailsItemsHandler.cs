using System.Collections.ObjectModel;
using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Inventory;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public sealed class ContainerDetailsItemsHandler
{
    private readonly IContainerDetailsQueryHandler containerDetailsQueries;
    private readonly IContainerItemQuantityService quantityService;
    private readonly ContainerItemPagingController itemPaging;
    private ContainerDetailsItemRowsViewModel? itemRows;

    public ContainerDetailsItemsHandler(
        IContainerDetailsQueryHandler containerDetailsQueries,
        IContainerItemQuantityService quantityService)
    {
        this.containerDetailsQueries = containerDetailsQueries;
        itemPaging = new ContainerItemPagingController(containerDetailsQueries);
        this.quantityService = quantityService;
    }

    public ObservableCollection<ItemWithPhotosViewModel> Items { get; } = new();
    public ObservableCollection<object> Rows { get; } = new();
    public bool IsEmpty => itemRows?.IsEmpty ?? true;

    public void Reset(object header)
    {
        itemPaging.Reset();
        itemRows = new ContainerDetailsItemRowsViewModel(header, Items, Rows);
        itemRows.Reset();
    }

    public void MarkComplete()
        => itemPaging.MarkComplete();

    public Task<ContainerDetailsResult?> GetDetailsAsync(string containerId)
        => containerDetailsQueries.GetDetailsAsync(containerId);

    public Task<int> GetDistinctItemCountAsync(string containerId)
        => containerDetailsQueries.GetDistinctItemCountAsync(containerId);

    public async Task<bool> ReloadAsync(
        string containerId,
        string? searchTerm,
        Func<ContainerItemInventoryEntry, ItemWithPhotosViewModel> createViewModel)
    {
        var rows = GetRows();
        rows.ClearItems();

        var page = await itemPaging.ReloadAsync(containerId, searchTerm);
        if (page.IsStale)
        {
            return false;
        }

        Append(page.Items, createViewModel);
        return true;
    }

    public async Task<bool> LoadMoreAsync(
        string containerId,
        Func<ContainerItemInventoryEntry, ItemWithPhotosViewModel> createViewModel)
    {
        var page = await itemPaging.LoadMoreAsync(containerId);
        if (page.IsStale)
        {
            return false;
        }

        Append(page.Items, createViewModel);
        return true;
    }

    public async Task<ContainerItemQuantityUpdateResult> SaveQuantityAsync(
        Container container,
        Guid itemId,
        int quantity)
    {
        var result = await quantityService.SaveQuantityAsync(container, itemId, quantity);
        var rows = GetRows();

        if (result.Removed)
        {
            rows.Remove(itemId);
        }
        else if (rows.Find(itemId) is { } item)
        {
            item.Quantity = quantity;
        }

        return result;
    }

    private void Append(
        IEnumerable<ContainerItemInventoryEntry> items,
        Func<ContainerItemInventoryEntry, ItemWithPhotosViewModel> createViewModel)
        => GetRows().Append(items, createViewModel);

    private ContainerDetailsItemRowsViewModel GetRows()
        => itemRows ?? throw new InvalidOperationException("The item handler must be reset before use.");
}