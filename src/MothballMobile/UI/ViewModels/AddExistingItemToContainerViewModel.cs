using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public partial class AddExistingItemToContainerViewModel : PagedListViewModelBase<Item, UnassignedItemViewModel>, IQueryAttributable
{
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly IImagePathResolver paths;
    private readonly INavigationService nav;

    [ObservableProperty]
    private string containerId = string.Empty;

    public AddExistingItemToContainerViewModel(
        IInventoryDomainRepository inventoryRepository,
        IImagePathResolver paths,
        INavigationService nav)
    {
        this.inventoryRepository = inventoryRepository;
        this.paths = paths;
        this.nav = nav;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue(NavigationParams.ContainerId, out var value) && value is string id)
        {
            ContainerId = id;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => InitializeAsync();

    protected override Task EnsureDummyData() => Task.CompletedTask;

    protected override UnassignedItemViewModel MapToViewModel(Item source)
        => new(source, paths, AssignAsync);

    protected override void OnViewModelAdded(UnassignedItemViewModel vm)
        => _ = vm.LoadImagesAsync();

    protected override Task<List<Item>> LoadAsync(int pageNumber, int pageSize)
        => inventoryRepository.GetUnassignedItemsWithPhotosAsync(pageNumber, pageSize);

    private async Task AssignAsync(Guid itemId)
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return;
        if (!Guid.TryParse(ContainerId, out var cid)) return;

        await RunCommandAsync(async () =>
        {
            await inventoryRepository.InsertItemContainerRelation(itemId, cid, quantity: 1);
            await nav.GoBackAsync();
        });
    }
}
