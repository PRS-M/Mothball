using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using Infrastructure.Services;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public partial class ItemsListViewModel : PagedListViewModelBase<Item, ItemViewModel>, IDisposable
{
    private readonly IImagePathResolver paths;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly INavigationService nav;
    private readonly IDebouncer debouncer;
    private readonly DemoDataSeeder? demoSeeder;

    [ObservableProperty]
    private string query = string.Empty;

    public ItemsListViewModel(
        IImagePathResolver paths,
        IInventoryQueryRepository inventoryQueries,
        INavigationService nav,
        IDebouncer? debouncer = null,
        DemoDataSeeder? demoSeeder = null)
    {
        this.paths = paths;
        this.inventoryQueries = inventoryQueries;
        this.nav = nav;
        this.debouncer = debouncer ?? new Debouncer(300);
        this.demoSeeder = demoSeeder;
    }

    private bool disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing && debouncer is IDisposable d)
        {
            d.Dispose();
        }
        disposed = true;
    }

    protected override async Task EnsureDummyData()
    {
        if (demoSeeder is not null)
        {
            await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: true);
        }
    }

    protected override ItemViewModel MapToViewModel(Item source)
    {
        return new ItemViewModel(source, paths, nav);
    }

    [RelayCommand]
    private Task RefreshAsync() => InitializeAsync();

    [RelayCommand]
    private async Task SearchAsync()
    {
        await RunCommandAsync(async () =>
        {
            await LoadQuerySearchAsync(Query);
        });
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            await SearchAsync();
            return;
        }

        Query = string.Empty;
        await SearchAsync();
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync(Guid itemId)
    {
        var id = itemId.ToString();
        return nav.GoToAsync(Infrastructure.NavigationRoutes.ItemDetails,
            new Dictionary<string, object> { [Infrastructure.NavigationParams.ItemId] = id });
    }

    [RelayCommand]
    private Task NavigateToAddItemAsync()
    {
        return nav.GoToAsync(Infrastructure.NavigationRoutes.AddItem);
    }

    private async Task LoadQuerySearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // restore normal paging
            await ReplaceWithFirstPagedAsync();
        }
        else
        {
            var items = await inventoryQueries.GetItemsWithPhotosAsync(query);
            ReplaceWithFullResultSet(items);
        }
    }

    partial void OnQueryChanged(string value)
    {
        // Debounce user typing to avoid flooding search
        debouncer.Debounce(() => MainThread.BeginInvokeOnMainThread(() => SearchAsync().Forget()));
    }

    protected override void OnViewModelAdded(ItemViewModel vm)
        => vm.LoadImageAsync().Forget();

    protected override Task<List<Item>> LoadAsync(int pageNumber, int pageSize)
        => inventoryQueries.GetAllItemsWithPhotosAsync(pageNumber, pageSize);
}
