using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Specifications;
using CoreApp.Interfaces;
using CoreApp.Entities.ContainerAggregate;
using Microsoft.Extensions.Logging.Abstractions;
using MothballMobile.Infrastructure;
using Infrastructure.Services;

namespace MothballMobile.UI.Features.Containers.ContainersList;

public enum ContainerListFilter
{
    All,
    Empty,
}

public partial class ContainerListViewModel : PagedListViewModelBase<Container, ContainerViewModel>, IDisposable
{
    private readonly IImagePathResolver imagePaths;
    private readonly INavigationService nav;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IDebouncer debouncer;
    private readonly IBackgroundTaskObserver backgroundTasks;

    private readonly DemoDataSeeder? demoSeeder; // optional in debug

    [ObservableProperty]
    private string query = string.Empty;

    private ContainerListFilter selectedFilter = ContainerListFilter.All;

    public static IReadOnlyList<ContainerListFilter> AvailableFilters { get; } = Enum.GetValues<ContainerListFilter>();

    public ContainerListFilter SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (!SetProperty(ref selectedFilter, value))
            {
                return;
            }

            MainThread.InvokeOnMainThreadAsync(SearchAsync)
                .FireAndForget(backgroundTasks, "Search containers");
        }
    }

    public ContainerListViewModel(
        IImagePathResolver imagePaths,
        IInventoryQueryRepository inventoryQueries,
        INavigationService nav,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null,
        DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.inventoryQueries = inventoryQueries;
        this.nav = nav;
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
        if (disposed)
        {
            return;
        }

        if (disposing && debouncer is IDisposable d)
        {
            d.Dispose();
        }

        disposed = true;
    }

    public ObservableCollection<ContainerViewModel> Containers => Items;

    protected override async Task EnsureDummyData()
    {
        if (demoSeeder is not null)
        {
            await demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
            await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: true);
        }
    }

    protected override async Task<List<Container>> LoadAsync(int pageNumber, int pageSize)
    {
        var specification = new ContainerListSpecification(
            Filter: ToQueryFilter(SelectedFilter),
            PageNumber: pageNumber,
            PageSize: pageSize);

        return await inventoryQueries.QueryContainersAsync(specification);
    }

    protected override ContainerViewModel MapToViewModel(Container source)
        => new ContainerViewModel(source, imagePaths, nav);

    protected override void OnViewModelAdded(ContainerViewModel vm)
        => vm.LoadImageAsync().FireAndForget(backgroundTasks, "Load container thumbnail");

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
    private Task NavigateToAddContainerAsync() => nav.GoToAsync(NavigationRoutes.AddContainer);

    [RelayCommand]
    private Task RefreshAsync() => InitializeAsync();

    private async Task LoadQuerySearchAsync(string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await ReplaceWithFirstPagedAsync();
            return;
        }

        var filtered = await inventoryQueries.QueryContainersAsync(
            new ContainerListSpecification(
                Filter: ToQueryFilter(SelectedFilter),
                SearchTerm: searchQuery));

        ReplaceWithFullResultSet(filtered);
    }

    partial void OnQueryChanged(string value)
    {
        debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(SearchAsync))
            .FireAndForget(backgroundTasks, "Search containers");
    }

    private static ContainerQueryFilter ToQueryFilter(ContainerListFilter filter)
        => filter == ContainerListFilter.Empty ? ContainerQueryFilter.Empty : ContainerQueryFilter.All;

}
