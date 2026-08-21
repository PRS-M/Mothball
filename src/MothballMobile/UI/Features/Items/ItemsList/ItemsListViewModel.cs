using CoreApp.Domain.Entities.InventoryAggregate;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Utilities;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using CoreApp.Application.Contracts;
using CoreApp.Application.Specifications;

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
    private readonly DemoDataSeeder? demoSeeder;

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
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null,
        DemoDataSeeder? demoSeeder = null)
        : base(backgroundTasks, debouncer)
    {
        this.paths = paths;
        this.itemListQueries = itemListQueries;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.demoSeeder = demoSeeder;
    }

    protected override string SearchOperationName => "Search items";

    /// <inheritdoc />
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

    protected override async Task LoadQuerySearchAsync(string? query)
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

    /// <inheritdoc />
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
