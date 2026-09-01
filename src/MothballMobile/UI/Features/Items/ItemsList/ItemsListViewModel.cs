using CoreApp.Domain.Entities.InventoryAggregate;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using CoreApp.Application.Contracts;
using CoreApp.Application.Specifications;
using MothballMobile.UI.Features.Items.Consumption;
using MothballMobile.UI.Features.Items.Quantity;

namespace MothballMobile.UI.Features.Items.ItemsList;

public enum ItemsListFilter
{
    All,
    Unassigned,
    Assigned,
}

public partial class ItemsListViewModel : SearchablePagedListViewModelBase<InventorySnapshot, ItemViewModel>
{
    private readonly IImagePathResolver paths;
    private readonly IItemsListQueryHandler itemListQueries;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly ItemQuantityEditCoordinator quantityEditCoordinator;
    private readonly ItemConsumptionCoordinator consumptionCoordinator;
    private readonly IDeleteItemCommandHandler deleteItemHandler;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private ItemsListFilter selectedFilter = ItemsListFilter.All;

    public static ReadOnlyCollection<ItemsListFilter> AvailableFilters { get; } = EnumValues.CreateReadOnly<ItemsListFilter>();

    public ItemsListFilter SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (!SetProperty(ref selectedFilter, value))
            {
                return;
            }

            MainThread.InvokeOnMainThreadAsync(SearchAsync)
                .FireAndForget(backgroundTasks, SearchOperationName);
        }
    }

    public ItemsListViewModel(
        IImagePathResolver paths,
        IItemsListQueryHandler itemListQueries,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        ItemQuantityEditCoordinator quantityEditCoordinator,
        ItemConsumptionCoordinator consumptionCoordinator,
        IDeleteItemCommandHandler deleteItemHandler,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null)
        : base(backgroundTasks, debouncer)
    {
        this.paths = paths;
        this.itemListQueries = itemListQueries;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.quantityEditCoordinator = quantityEditCoordinator;
        this.consumptionCoordinator = consumptionCoordinator;
        this.deleteItemHandler = deleteItemHandler;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
    }

    protected override string SearchOperationName => "Search items";

    /// <inheritdoc />
    protected override Task EnsureDummyData() => Task.CompletedTask;

    protected override ItemViewModel MapToViewModel(InventorySnapshot source)
    {
        return new ItemViewModel(
            source,
            paths,
            nav,
            applicationSettings.IsAdvancedMode,
            EditQuantityAsync,
            UseAsync,
            DeleteAsync);
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync(Guid itemId)
    {
        return nav.GoToAsync(Infrastructure.NavigationRoutes.ItemDetails,
            new Infrastructure.Navigation.ItemDetailsNavigationRequest(itemId));
    }

    [RelayCommand]
    private Task NavigateToAddItemAsync()
    {
        return nav.GoToAsync(Infrastructure.NavigationRoutes.AddItem);
    }

    /// <inheritdoc />
    protected override void OnViewModelAdded(ItemViewModel vm)
        => vm.LoadImageAsync().FireAndForget(backgroundTasks, "Load item thumbnail");

    protected override Task<List<InventorySnapshot>> LoadPageAsync(string? query, int pageNumber, int pageSize)
        => itemListQueries.QueryAsync(
            GetItemQueryFilter(),
            query,
            pageNumber,
            pageSize);

    private ItemQueryFilter GetItemQueryFilter()
        => SelectedFilter switch
        {
            ItemsListFilter.Assigned => ItemQueryFilter.Assigned,
            ItemsListFilter.Unassigned => ItemQueryFilter.Unassigned,
            _ => ItemQueryFilter.All,
        };

    private async Task EditQuantityAsync(ItemViewModel item)
    {
        try
        {
            var execution = await quantityEditCoordinator.ExecuteAsync(item.Item.ItemId);
            ApplyInventoryUpdate(item, execution?.Update, execution?.Inventory);
        }
        catch (Exception ex)
        {
            await popup.ShowAlertAsync(popupDefinitions.InventoryQuantityUpdateFailed(ex.Message));
        }
    }

    private async Task UseAsync(ItemViewModel item)
    {
        try
        {
            var execution = await consumptionCoordinator.ExecuteAsync(item.Item.ItemId);
            ApplyInventoryUpdate(item, execution?.Update, execution?.Inventory);
        }
        catch (Exception ex)
        {
            await popup.ShowAlertAsync(popupDefinitions.InventoryQuantityUpdateFailed(ex.Message));
        }
    }

    private Task DeleteAsync(ItemViewModel item)
        => popup.ConfirmAndRunAsync(popupDefinitions.DeleteItem(), async () =>
        {
            await deleteItemHandler.DeleteAsync(item.Item.ItemId.ToString());
            Items.Remove(item);
        });

    private void ApplyInventoryUpdate(
        ItemViewModel item,
        ItemInventoryUpdateResult? update,
        InventorySnapshot? inventory)
    {
        if (update is null)
        {
            return;
        }

        if (update.ItemDeleted || inventory is null)
        {
            Items.Remove(item);
            return;
        }

        item.UpdateQuantities(update.TotalQuantity, update.AssignedQuantity, update.UnassignedQuantity);
        bool stillMatches = SelectedFilter switch
        {
            ItemsListFilter.Assigned => update.AssignedQuantity > 0,
            ItemsListFilter.Unassigned => update.UnassignedQuantity > 0,
            _ => true,
        };
        if (!stillMatches)
        {
            Items.Remove(item);
        }
    }

}
