using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using Infrastructure.Services;

namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public partial class AssociateItemWithContainerViewModel : PagedListViewModelBase<Container, SelectableContainerViewModel>, IQueryAttributable
{
    private readonly IImagePathResolver imagePaths;
    private readonly IContainerAssociationQueryHandler associationQueries;
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IAssignItemToContainerCommandHandler assignItemToContainer;
    private readonly Infrastructure.INavigationService nav;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly DemoDataSeeder? demoSeeder;

    private string? itemId;
    private int unassignedQuantity;

    public AssociateItemWithContainerViewModel(
        IImagePathResolver imagePaths,
        IContainerAssociationQueryHandler associationQueries,
        IItemDetailsQueryHandler itemDetailsQueries,
        IAssignItemToContainerCommandHandler assignItemToContainer,
        Infrastructure.INavigationService nav,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IBackgroundTaskObserver backgroundTasks,
        DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.associationQueries = associationQueries;
        this.itemDetailsQueries = itemDetailsQueries;
        this.assignItemToContainer = assignItemToContainer;
        this.nav = nav;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.backgroundTasks = backgroundTasks;
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
        => new(source, imagePaths, AssociateWithContainerAsync);

    protected override void OnViewModelAdded(SelectableContainerViewModel vm)
        => vm.LoadImagesAsync().FireAndForget(backgroundTasks, "Load selectable container images");

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
}
