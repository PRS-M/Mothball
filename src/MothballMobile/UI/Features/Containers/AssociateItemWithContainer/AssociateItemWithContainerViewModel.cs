using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Infrastructure.Services;

namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public partial class AssociateItemWithContainerViewModel : PagedListViewModelBase<Container, SelectableContainerViewModel>, IQueryAttributable, IDisposable
{
    private readonly IImagePathResolver imagePaths;
    private readonly IContainerAssociationQueryHandler associationQueries;
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IAssignItemToContainerCommandHandler assignItemToContainer;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly DemoDataSeeder? demoSeeder;
    private readonly IDebouncer debouncer;

    private string? itemId;
    private int unassignedQuantity;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    public AssociateItemWithContainerViewModel(
        IImagePathResolver imagePaths,
        IContainerAssociationQueryHandler associationQueries,
        IItemDetailsQueryHandler itemDetailsQueries,
        IAssignItemToContainerCommandHandler assignItemToContainer,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null,
        DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.associationQueries = associationQueries;
        this.itemDetailsQueries = itemDetailsQueries;
        this.assignItemToContainer = assignItemToContainer;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.backgroundTasks = backgroundTasks;
        this.debouncer = debouncer ?? new Debouncer(300, NullLogger<Debouncer>.Instance);
        this.demoSeeder = demoSeeder;
    }

    public ObservableCollection<SelectableContainerViewModel> Containers => Items;

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

    protected override async Task EnsureDummyData()
    {
        if (demoSeeder is not null)
        {
            await demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
        }
    }

    protected override Task<List<Container>> LoadAsync(int pageNumber, int pageSize)
        => associationQueries.QueryContainersAsync(pageNumber, pageSize);

    protected override SelectableContainerViewModel MapToViewModel(Container source)
        => new(source, imagePaths, AssociateWithContainerAsync, applicationSettings.IsAdvancedMode);

    protected override void OnViewModelAdded(SelectableContainerViewModel vm)
        => vm.LoadImagesAsync().FireAndForget(backgroundTasks, "Load selectable container images");

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

            var containers = await associationQueries.QueryContainersAsync(SearchQuery);
            ReplaceWithFullResultSet(containers);
        });
    }

    partial void OnSearchQueryChanged(string value)
    {
        debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(ApplySearchAsync))
            .FireAndForget(backgroundTasks, "Search association containers");
    }

    private async Task AssociateWithContainerAsync(Guid containerId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        if (!Guid.TryParse(itemId, out var parsedItemId)) return;

        await RunCommandAsync(async () =>
        {
            var availableQuantity = await GetAvailableQuantityAsync(parsedItemId, containerId);
            if (availableQuantity <= 0)
            {
                await nav.GoBackAsync();
                return;
            }

            if (!applicationSettings.IsAdvancedMode)
            {
                await assignItemToContainer.AssignAsync(parsedItemId, containerId);
                await nav.GoBackAsync();
                return;
            }

            var selectedQuantity = await popup.PickNumberAsync(
                popupDefinitions.AssociateUnassignedQuantity(availableQuantity));
            if (selectedQuantity is null)
            {
                return;
            }

            await assignItemToContainer.AssignAsync(parsedItemId, containerId, selectedQuantity.Value);
            await nav.GoBackAsync();
        });
    }

    private async Task<int> GetAvailableQuantityAsync(Guid parsedItemId, Guid selectedContainerId)
    {
        var details = await itemDetailsQueries.GetDetailsAsync(parsedItemId.ToString());
        if (details is null)
        {
            return unassignedQuantity;
        }

        unassignedQuantity = details.Inventory.UnassignedQuantity;
        var currentContainerQuantity = details.Inventory.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == selectedContainerId)?.Quantity ?? 0;
        return unassignedQuantity + currentContainerQuantity;
    }

    public void Dispose()
    {
        if (debouncer is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
