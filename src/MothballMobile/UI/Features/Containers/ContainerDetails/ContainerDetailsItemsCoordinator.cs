using System.Collections.ObjectModel;
using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Containers.ContainerDetails;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;
using MothballMobile.UI.Features.Items.Consumption;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public sealed class ContainerDetailsItemsCoordinator
{
    private readonly IContainerDetailsHandler containerDetailsHandler;
    private readonly IImagePathResolver paths;
    private readonly INavigationService navigation;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly ItemConsumptionCoordinator consumptionCoordinator;
    private readonly ContainerItemPagingController itemPaging;
    private ContainerDetailsItemRowsViewModel? itemRows;
    private IContainerDetailsHeader? header;
    private bool skipNextInitialization;

    public ContainerDetailsItemsCoordinator(
        IContainerDetailsHandler containerDetailsHandler,
        IContainerDetailsQueryHandler containerDetailsQueries,
        IImagePathResolver paths,
        INavigationService navigation,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ItemConsumptionCoordinator consumptionCoordinator,
        IBackgroundTaskObserver backgroundTasks)
    {
        this.containerDetailsHandler = containerDetailsHandler;
        itemPaging = new ContainerItemPagingController(containerDetailsQueries);
        this.paths = paths;
        this.navigation = navigation;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.consumptionCoordinator = consumptionCoordinator;
        this.backgroundTasks = backgroundTasks;
    }

    public ObservableCollection<ItemWithPhotosViewModel> Items { get; } = new();
    public ObservableCollection<object> Rows { get; } = new();
    public bool IsEmpty => itemRows?.IsEmpty ?? true;

    public async Task<ContainerDetailsSummary?> LoadSummaryAsync(
        string containerId,
        IContainerDetailsHeader header)
    {
        Reset(header);

        var summary = await containerDetailsHandler.GetSummaryAsync(containerId);
        if (summary is null)
        {
            itemPaging.MarkComplete();
            return null;
        }

        return summary;
    }

    public void Reset(IContainerDetailsHeader header)
    {
        this.header = header;
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

        if (update.Inventory.RemovedFromContainer)
        {
            rows.Remove(itemId);
        }
        else if (rows.Find(itemId) is { } item)
        {
            item.Quantity = quantity;
            item.UpdateQuantities(update.Inventory.TotalQuantity, update.Inventory.AssignedQuantity, update.Inventory.UnassignedQuantity);
        }

        // Item counts depend on the whole container, so refresh the header from the latest summary.
        if (header is not null)
        {
            header.ItemTypesCount = update.Summary.ItemTypesCount;
            header.TotalItemCount = showQuantityManagement ? update.Summary.TotalItemCount : update.Summary.ItemTypesCount;
        }

        return update.Summary;
    }

    public async Task ConsumeAsync(
        Container container,
        Guid itemId,
        Guid preferredContainerId,
        bool showQuantityManagement)
    {
        var execution = await consumptionCoordinator.ExecuteAsync(itemId, preferredContainerId);
        if (execution is null)
        {
            return;
        }

        var rows = GetRows();
        var allocation = execution.Inventory?.Allocations.FirstOrDefault(candidate =>
            candidate.ContainerId == container.ContainerId);
        if (execution.Update.ItemDeleted || allocation is null)
        {
            rows.Remove(itemId);
        }
        else if (rows.Find(itemId) is { } item)
        {
            item.Quantity = allocation.Quantity;
            item.UpdateQuantities(
                execution.Update.TotalQuantity,
                execution.Update.AssignedQuantity,
                execution.Update.UnassignedQuantity);
        }

        var summary = await containerDetailsHandler.GetSummaryAsync(container.ContainerId.ToString());
        if (summary is not null && header is not null)
        {
            header.ItemTypesCount = summary.ItemTypesCount;
            header.TotalItemCount = showQuantityManagement ? summary.TotalItemCount : summary.ItemTypesCount;
        }
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
            async (itemId, preferredContainerId) => await ConsumeAsync(
                container,
                itemId,
                preferredContainerId,
                showQuantityManagement),
            SkipNextInitialization);
        itemViewModel.LoadImagesAsync().FireAndForget(backgroundTasks, "Load container item images");
        return itemViewModel;
    }

    private void SkipNextInitialization()
        => skipNextInitialization = true;

    private ContainerDetailsItemRowsViewModel GetRows()
        => itemRows ?? throw new InvalidOperationException("The item coordinator must be reset before use.");
}
