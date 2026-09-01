using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Application.Utilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace MothballMobile.UI.Features.Containers.ContainersList;

public enum ContainerListFilter
{
    All,
    Empty,
}

public partial class ContainerListViewModel : SearchablePagedListViewModelBase<Container, ContainerViewModel>
{
    private readonly IImagePathResolver imagePaths;
    private readonly INavigationService nav;
    private readonly IContainerListQueryHandler containerListQueries;
    private readonly IApplicationSettings applicationSettings;
    private readonly IInventoryChangeTracker inventoryChanges;

    private ContainerListFilter selectedFilter = ContainerListFilter.All;

    public static ReadOnlyCollection<ContainerListFilter> AvailableFilters { get; } = EnumValues.CreateReadOnly<ContainerListFilter>();

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
                .FireAndForget(backgroundTasks, SearchOperationName);
        }
    }

    public ContainerListViewModel(
        IImagePathResolver imagePaths,
        IContainerListQueryHandler containerListQueries,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IInventoryChangeTracker inventoryChanges,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null,
        IPagedListLoadDiagnostics? loadDiagnostics = null)
        : base(backgroundTasks, debouncer, pageSize: 10, loadDiagnostics: loadDiagnostics)
    {
        this.imagePaths = imagePaths;
        this.containerListQueries = containerListQueries;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.inventoryChanges = inventoryChanges;
    }

    protected override string SearchOperationName => "Search containers";
    protected override long DataRevision => inventoryChanges.Revision;
    protected override string LoadVariant => $"{SelectedFilter}:{base.LoadVariant}";

    public ObservableCollection<ContainerViewModel> Containers => Items;

    protected override Task<List<Container>> LoadPageAsync(string? query, int pageNumber, int pageSize)
        => containerListQueries.QueryAsync(
            IsEmptyFilterSelected(),
            query,
            pageNumber,
            pageSize);

    protected override ContainerViewModel MapToViewModel(Container source)
        => new ContainerViewModel(source, imagePaths, nav, applicationSettings.IsAdvancedMode);

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => nav.GoToAsync(NavigationRoutes.AddContainer);

    private bool IsEmptyFilterSelected()
        => SelectedFilter == ContainerListFilter.Empty;

}
