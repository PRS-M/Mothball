using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.ContainerAggregate;
using MothballMobile.Infrastructure.Scanning;

namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public partial class AssociateItemWithContainerViewModel : PagedListViewModelBase<Container, SelectableContainerViewModel>, IQueryAttributable, IDisposable
{
    private readonly AssociateItemWithContainerCoordinator coordinator;
    private readonly IContainerItemAssociationHandler itemAssociationHandler;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly IBarcodeScanSession barcodeScanner;
    private readonly IInventoryQueryRepository inventoryQueries;
    private string? itemId;
    private int unassignedQuantity;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    public AssociateItemWithContainerViewModel(
        AssociateItemWithContainerCoordinator coordinator,
        IContainerItemAssociationHandler itemAssociationHandler,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IBarcodeScanSession barcodeScanner,
        IInventoryQueryRepository inventoryQueries)
        : base(pageSize: 10)
    {
        this.coordinator = coordinator;
        this.itemAssociationHandler = itemAssociationHandler;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.barcodeScanner = barcodeScanner;
        this.inventoryQueries = inventoryQueries;
    }

    public ObservableCollection<SelectableContainerViewModel> Containers => Items;

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParams.ItemId, out var value) && value is string id && !string.IsNullOrWhiteSpace(id))
        {
            itemId = id;
        }

        if (query.TryGetValue(NavigationParams.UnassignedQuantity, out var quantityValue))
        {
            unassignedQuantity = quantityValue switch
            {
                int quantity => quantity,
                string raw when int.TryParse(raw, out var parsed) => parsed,
                _ => unassignedQuantity,
            };
        }
    }

    /// <inheritdoc />
    protected override Task<List<Container>> LoadAsync(int pageNumber, int pageSize)
        => coordinator.LoadPageAsync(pageNumber, pageSize);

    /// <inheritdoc />
    protected override SelectableContainerViewModel MapToViewModel(Container source)
        => coordinator.CreateContainerViewModel(source, AssociateWithContainerAsync);

    [RelayCommand]
    private async Task ApplySearchAsync()
    {
        await RunCommandAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                await ReplaceWithFirstPagedAsync();
                return;
            }

            var containers = await coordinator.SearchAsync(SearchQuery);
            ReplaceWithFullResultSet(containers);
        });
    }

    partial void OnSearchQueryChanged(string value)
        => coordinator.DebounceSearch(ApplySearchAsync);

    [RelayCommand]
    private async Task ScanContainerAsync()
    {
        var barcode = await barcodeScanner.ScanAsync();
        if (barcode is null)
        {
            return;
        }

        var owner = await inventoryQueries.FindBarcodeAsync(barcode.Value);
        if (owner?.OwnerKind == BarcodeOwnerKind.Container)
        {
            await AssociateWithContainerAsync(owner.OwnerId);
        }
    }

    private async Task AssociateWithContainerAsync(Guid containerId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        if (!Guid.TryParse(itemId, out var parsedItemId)) return;

        await RunCommandAsync(async () =>
        {
            var availableQuantity = await itemAssociationHandler.GetAvailableQuantityAsync(
                parsedItemId,
                containerId,
                unassignedQuantity);
            if (availableQuantity <= 0)
            {
                await nav.GoBackAsync();
                return;
            }

            if (!applicationSettings.IsAdvancedMode)
            {
                var association = await itemAssociationHandler.TryAssociateAsync(
                    parsedItemId,
                    containerId,
                    quantity: 1,
                    unassignedQuantity);
                if (association.Associated)
                {
                    await nav.GoBackAsync();
                }
                return;
            }

            var selectedQuantity = await popup.PickNumberAsync(
                popupDefinitions.AssociateUnassignedQuantity(availableQuantity));
            if (selectedQuantity is null)
            {
                return;
            }

            var associationResult = await itemAssociationHandler.TryAssociateAsync(
                parsedItemId,
                containerId,
                selectedQuantity.Value,
                unassignedQuantity);
            if (associationResult.Associated)
            {
                await nav.GoBackAsync();
            }
        });
    }

    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            coordinator.Dispose();
        }

        disposed = true;
    }
}
