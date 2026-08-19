using System.Collections.ObjectModel;
using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Inventory;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public sealed class ContainerDetailsItemsCoordinator
{
    private readonly IContainerDetailsHandler containerDetailsHandler;
    private readonly IImagePathResolver paths;
    private readonly INavigationService navigation;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly ContainerItemPagingController itemPaging;
    private ContainerDetailsItemRowsViewModel? itemRows;
    private bool skipNextInitialization;

    public ContainerDetailsItemsCoordinator(
        IContainerDetailsHandler containerDetailsHandler,
        IContainerDetailsQueryHandler containerDetailsQueries,
        IImagePathResolver paths,
        INavigationService navigation,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IBackgroundTaskObserver backgroundTasks)
    {
        this.containerDetailsHandler = containerDetailsHandler;
        itemPaging = new ContainerItemPagingController(containerDetailsQueries);
        this.paths = paths;
        this.navigation = navigation;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.backgroundTasks = backgroundTasks;
    }

    public ObservableCollection<ItemWithPhotosViewModel> Items { get; } = new();
    public ObservableCollection<object> Rows { get; } = new();
    public bool IsEmpty => itemRows?.IsEmpty ?? true;

    public async Task<ContainerDetailsSummary?> InitializeAsync(
        string containerId,
        object header,
        bool showQuantityManagement)
    {
        Reset(header);

        var summary = await containerDetailsHandler.GetSummaryAsync(containerId);
        if (summary is null)
        {
            itemPaging.MarkComplete();
            return null;
        }

        await ReloadAsync(containerId, summary.Container, searchTerm: null, showQuantityManagement);
        return summary;
    }

    public void Reset(object header)
    {
        itemPaging.Reset();
        itemRows = new ContainerDetailsItemRowsViewModel(header, Items, Rows);
        itemRows.Reset();
        skipNextInitialization = false;
    }

    public async Task<bool> ReloadAsync(
        string containerId,
        Container container,
        string? searchTerm,
        bool showQuantityManagement)
    {
        var rows = GetRows();
        rows.ClearItems();

        var page = await itemPaging.ReloadAsync(containerId, searchTerm);
        if (page.IsStale)
        {
            return false;
        }

        Append(page.Items, container, showQuantityManagement);
        return true;
    }

    public async Task<bool> LoadMoreAsync(
        string containerId,
        Container container,
        bool showQuantityManagement)
    {
        var page = await itemPaging.LoadMoreAsync(containerId);
        if (page.IsStale)
        {
            return false;
        }

        Append(page.Items, container, showQuantityManagement);
        return true;
    }

    public async Task<ContainerDetailsSummary> SaveQuantityAsync(
        Container container,
        Guid itemId,
        int quantity,
        bool showQuantityManagement)
    {
        var update = await containerDetailsHandler.SaveItemQuantityAsync(container, itemId, quantity);
        var rows = GetRows();

        if (update.Removed)
        {
            rows.Remove(itemId);
        }
        else if (rows.Find(itemId) is { } item)
        {
            item.Quantity = quantity;
        }

        return update.Summary;
    }

    public bool TryConsumeSkipNextInitialization()
    {
        if (!skipNextInitialization)
        {
            return false;
        }

        skipNextInitialization = false;
        return true;
    }

    private void Append(
        IEnumerable<ContainerItemInventoryEntry> items,
        Container container,
        bool showQuantityManagement)
        => GetRows().Append(items, entry => CreateItemViewModel(entry, container, showQuantityManagement));

    private ItemWithPhotosViewModel CreateItemViewModel(
        ContainerItemInventoryEntry entry,
        Container container,
        bool showQuantityManagement)
    {
        var itemViewModel = new ItemWithPhotosViewModel(
            entry,
            container.ContainerId,
            paths,
            navigation,
            popup,
            popupDefinitions,
            container.ContainerId.ToString(),
            showQuantityManagement,
            async (itemId, quantity) => await SaveQuantityAsync(container, itemId, quantity, showQuantityManagement),
            SkipNextInitialization);
        itemViewModel.LoadImagesAsync().FireAndForget(backgroundTasks, "Load container item images");
        return itemViewModel;
    }

    private void SkipNextInitialization()
        => skipNextInitialization = true;

    private ContainerDetailsItemRowsViewModel GetRows()
        => itemRows ?? throw new InvalidOperationException("The item coordinator must be reset before use.");
}