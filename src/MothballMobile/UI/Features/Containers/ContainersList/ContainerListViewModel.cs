using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Entities.ContainerAggregate;
using MothballMobile.Infrastructure;
using Infrastructure.Services;
using System.Linq;

namespace MothballMobile.UI.Features.Containers.ContainersList;
public partial class ContainerListViewModel : PagedListViewModelBase<Container, ContainerViewModel>, IDisposable
{
    private readonly IImagePathResolver imagePaths;
    private readonly INavigationService nav;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IDebouncer debouncer;

    private readonly DemoDataSeeder? demoSeeder; // optional in debug

    [ObservableProperty]
    private string query = string.Empty;

    public ContainerListViewModel(IImagePathResolver imagePaths, IInventoryQueryRepository inventoryQueries, INavigationService nav, IDebouncer? debouncer = null, DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
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
        => await inventoryQueries.GetAllContainersAsync(pageNumber, pageSize);

    protected override ContainerViewModel MapToViewModel(Container source)
        => new ContainerViewModel(source, imagePaths, nav);

    protected override void OnViewModelAdded(ContainerViewModel vm)
        => _ = vm.LoadImageAsync();

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

        var normalized = searchQuery.Trim();
        var allContainers = await inventoryQueries.GetAllContainersAsync();
        var filtered = allContainers.Where(container =>
            container.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
            container.Notes.Contains(normalized, StringComparison.OrdinalIgnoreCase));

        ReplaceWithFullResultSet(filtered);
    }

    partial void OnQueryChanged(string value)
    {
        debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(SearchAsync)).FireAndForget();
    }
}
