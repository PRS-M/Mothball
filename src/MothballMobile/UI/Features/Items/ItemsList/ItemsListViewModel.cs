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
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.Infrastructure.BarcodeDocuments;

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
    private readonly IInventoryChangeTracker inventoryChanges;
    private readonly BarcodeLookupCoordinator barcodeLookup;
    private readonly IBarcodeShareService? barcodeShare;
    private ItemsListFilter selectedFilter = ItemsListFilter.All;

    [ObservableProperty]
    private bool isSelectionMode;

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
        IInventoryChangeTracker inventoryChanges,
        BarcodeLookupCoordinator barcodeLookup,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null,
        IPagedListLoadDiagnostics? loadDiagnostics = null,
        IBarcodeShareService? barcodeShare = null)
        : base(backgroundTasks, debouncer, loadDiagnostics: loadDiagnostics)
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
        this.inventoryChanges = inventoryChanges;
        this.barcodeLookup = barcodeLookup ?? throw new ArgumentNullException(nameof(barcodeLookup));
        this.barcodeShare = barcodeShare;
    }

    protected override string SearchOperationName => "Search items";
    protected override long DataRevision => inventoryChanges.Revision;
    protected override string LoadVariant => $"{SelectedFilter}:{base.LoadVariant}";

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

    public int SelectedCount => Items.Count(item => item.IsSelected);
    public bool HasSelection => SelectedCount > 0;

    protected override void OnViewModelAdded(ItemViewModel vm)
    {
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ItemViewModel.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelection));
            }
        };
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

    [RelayCommand]
    private Task ScanToFindAsync() => RunCommandAsync(barcodeLookup.ScanAndNavigateAsync, rethrowOnError: false);

    [RelayCommand]
    private void EnterSelectionMode() => IsSelectionMode = true;

    [RelayCommand]
    private void SelectAllLoaded()
    {
        foreach (var item in Items)
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private void CancelSelection()
    {
        ClearSelection();
        IsSelectionMode = false;
    }

    [RelayCommand]
    private Task ShareSelectedAsync()
    {
        var labels = Items
            .Where(item => item.IsSelected && item.Item.Barcode is not null)
            .Select(item => new BarcodeLabelData(
                item.Name,
                item.Item.Barcode!.Value,
                item.Item.Barcode.Symbology))
            .ToArray();

        if (labels.Length == 0)
        {
            return RunCommandAsync(
                () => Task.FromException(new InvalidOperationException(LocalizationManager.Current.Get("No barcode labels found."))),
                rethrowOnError: false);
        }

        return barcodeShare is null
            ? Task.CompletedTask
            : RunCommandAsync(
                () => barcodeShare.ShareAsync(labels, "Share item barcodes"),
                rethrowOnError: false);
    }

    [RelayCommand]
    private Task ShareAllMatchingAsync()
        => RunCommandAsync(async () =>
        {
            var items = await itemListQueries.QueryAsync(GetItemQueryFilter(), Query, null, null);
            var labels = items.Where(item => item.Item.Barcode is not null)
                .Select(item => new BarcodeLabelData(item.Item.Name, item.Item.Barcode!.Value, item.Item.Barcode.Symbology))
                .ToArray();
            if (labels.Length == 0)
            {
                throw new InvalidOperationException(LocalizationManager.Current.Get("No barcode labels found."));
            }

            if (barcodeShare is not null)
            {
                await barcodeShare.ShareAsync(labels, "Share item barcodes");
            }
        }, rethrowOnError: false);

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
            var execution = await quantityEditCoordinator.ExecuteAsync(
                item.Item.ItemId,
                decreasePreference: ItemQuantityDecreasePreference.UnassignedFirst);
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
