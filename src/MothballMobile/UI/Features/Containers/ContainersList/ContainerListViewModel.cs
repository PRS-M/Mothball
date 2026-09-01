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
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null)
        : base(backgroundTasks, debouncer, pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.containerListQueries = containerListQueries;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
    }

    protected override string SearchOperationName => "Search containers";

    public ObservableCollection<ContainerViewModel> Containers => Items;

    /// <inheritdoc />
    protected override Task EnsureDummyData() => Task.CompletedTask;

    protected override async Task<List<Container>> LoadAsync(int pageNumber, int pageSize)
        => await containerListQueries.QueryAsync(IsEmptyFilterSelected(), pageNumber: pageNumber, pageSize: pageSize);

    protected override ContainerViewModel MapToViewModel(Container source)
        => new ContainerViewModel(source, imagePaths, nav, applicationSettings.IsAdvancedMode);

    /// <inheritdoc />
    protected override void OnViewModelAdded(ContainerViewModel vm)
        => vm.LoadImageAsync().FireAndForget(backgroundTasks, "Load container thumbnail");

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => nav.GoToAsync(NavigationRoutes.AddContainer);

    protected override async Task LoadQuerySearchAsync(string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await ReplaceWithFirstPagedAsync();
            return;
        }

        var filtered = await containerListQueries.QueryAsync(IsEmptyFilterSelected(), searchQuery);

        ReplaceWithFullResultSet(filtered);
    }

    private bool IsEmptyFilterSelected()
        => SelectedFilter == ContainerListFilter.Empty;

}
