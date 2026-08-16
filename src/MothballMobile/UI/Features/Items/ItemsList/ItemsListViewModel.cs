using CoreApp.Entities.Inventory;
﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using CoreApp.Contracts;
using CoreApp.Specifications;

namespace MothballMobile.UI.Features.Items.ItemsList;

public enum ItemsListFilter
{
    All,
    Unassigned,
    Assigned,
}

public partial class ItemsListViewModel : PagedListViewModelBase<InventorySnapshot, ItemViewModel>, IDisposable
{
    private readonly IImagePathResolver paths;
    private readonly IItemsListQueryHandler itemListQueries;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly IDebouncer debouncer;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly DemoDataSeeder? demoSeeder;

    [ObservableProperty]
    private string query = string.Empty;

    private ItemsListFilter selectedFilter = ItemsListFilter.All;

    public static IReadOnlyList<ItemsListFilter> AvailableFilters { get; } = Enum.GetValues<ItemsListFilter>();

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
                .FireAndForget(backgroundTasks, "Search items");
        }
    }

    public ItemsListViewModel(
        IImagePathResolver paths,
        IItemsListQueryHandler itemListQueries,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null,
        DemoDataSeeder? demoSeeder = null)
    {
        this.paths = paths;
        this.itemListQueries = itemListQueries;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.backgroundTasks = backgroundTasks;
        this.debouncer = debouncer ?? new Debouncer(300, NullLogger<Debouncer>.Instance);
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

    protected override ItemViewModel MapToViewModel(InventorySnapshot source)
    {
        return new ItemViewModel(source, paths, nav, applicationSettings.IsAdvancedMode);
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
            var items = await itemListQueries.QueryAsync(GetItemQueryFilter(), query);

            ReplaceWithFullResultSet(items);
        }
    }

    partial void OnQueryChanged(string value)
    {
        // Debounce user typing to avoid flooding search
        debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(SearchAsync))
            .FireAndForget(backgroundTasks, "Search items");
    }

    protected override void OnViewModelAdded(ItemViewModel vm)
        => vm.LoadImageAsync().FireAndForget(backgroundTasks, "Load item thumbnail");

    protected override Task<List<InventorySnapshot>> LoadAsync(int pageNumber, int pageSize)
        => itemListQueries.QueryAsync(GetItemQueryFilter(), pageNumber: pageNumber, pageSize: pageSize);

    private ItemQueryFilter GetItemQueryFilter()
        => SelectedFilter switch
        {
            ItemsListFilter.Assigned => ItemQueryFilter.Assigned,
            ItemsListFilter.Unassigned => ItemQueryFilter.Unassigned,
            _ => ItemQueryFilter.All,
        };

}
