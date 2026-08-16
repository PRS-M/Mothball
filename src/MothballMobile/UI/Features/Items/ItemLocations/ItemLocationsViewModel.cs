using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Interfaces;

namespace MothballMobile.UI.Features.Items.ItemLocations;

public partial class ItemLocationsViewModel : BaseViewModel, IQueryAttributable, IInitializable
{
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IImagePathResolver imagePaths;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;

    [ObservableProperty]
    private string itemId = string.Empty;

    [ObservableProperty]
    private string itemName = string.Empty;

    public ObservableCollection<ItemLocationViewModel> Locations { get; } = new();

    public ItemLocationsViewModel(
        IItemDetailsQueryHandler itemDetailsQueries,
        IInventoryQueryRepository inventoryQueries,
        IImagePathResolver imagePaths,
        INavigationService nav,
        IApplicationSettings applicationSettings)
    {
        this.itemDetailsQueries = itemDetailsQueries ?? throw new ArgumentNullException(nameof(itemDetailsQueries));
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.imagePaths = imagePaths ?? throw new ArgumentNullException(nameof(imagePaths));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParams.ItemId, out var value)
            && value is string id
            && !string.IsNullOrWhiteSpace(id))
        {
            ItemId = id;
        }
    }

    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            return Task.CompletedTask;
        }

        return RunCommandAsync(async () =>
        {
            Locations.Clear();
            var details = await itemDetailsQueries.GetDetailsAsync(ItemId);
            if (details is null)
            {
                ItemName = "Item not found";
                return;
            }

            ItemName = details.Inventory.Item.Name;
            foreach (var allocation in details.Inventory.Allocations.Where(allocation => allocation.Quantity > 0))
            {
                var container = await inventoryQueries.GetContainerAsync(allocation.ContainerId.ToString());
                if (container is null)
                {
                    continue;
                }

                var location = new ItemLocationViewModel(
                    container,
                    allocation,
                    imagePaths,
                    nav,
                    applicationSettings.IsAdvancedMode);
                await location.LoadImagesAsync();
                Locations.Add(location);
            }
        });
    }
}
