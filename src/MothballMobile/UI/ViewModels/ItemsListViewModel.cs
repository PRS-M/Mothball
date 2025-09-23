using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using Infrastructure.Services;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public partial class ItemsListViewModel : PagedListViewModelBase<Item, ItemViewModel>, IDisposable, IInitializable
{
    private readonly IImagePathResolver paths;
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly INavigationService nav;
    private readonly IDebouncer debouncer;
    private readonly DemoDataSeeder? demoSeeder;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string query = string.Empty;

    public ItemsListViewModel(
        IImagePathResolver paths,
        IInventoryDomainRepository inventoryRepository,
        INavigationService nav,
        IDebouncer? debouncer = null,
        DemoDataSeeder? demoSeeder = null)
    {
        this.paths = paths;
        this.inventoryRepository = inventoryRepository;
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
    private Task RefreshAsync()
    {
        return InitializeAsync();
    }

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
        Items.Clear();
        List<Item>? items;

        if (string.IsNullOrWhiteSpace(query))
        {
            currentPage = 0;
            items = await inventoryRepository.GetAllItemsWithPhotosAsync(currentPage, pageSize);
        }
        else
        {
            items = await inventoryRepository.GetItemsWithPhotosAsync(query);
        }

        foreach (var item in items)
        {
            var vm = new ItemViewModel(item, paths, nav);
            Items.Add(vm);
            _ = vm.LoadImageAsync();
        }
    }

    partial void OnQueryChanged(string value)
    {
        // Debounce user typing
        debouncer.Debounce(() => MainThread.BeginInvokeOnMainThread(() => _ = SearchAsync()));
    }

    protected override void OnViewModelAdded(ItemViewModel vm)
        => _ = vm.LoadImageAsync();

    protected override Task<List<Item>> LoadAsync(int pageNumber, int pageSize)
        => inventoryRepository.GetAllItemsWithPhotosAsync(pageNumber, pageSize);
}
