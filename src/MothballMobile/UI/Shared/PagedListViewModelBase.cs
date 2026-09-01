using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace MothballMobile.UI.Shared;

public abstract partial class PagedListViewModelBase<TSource, TViewModel> : BaseViewModel, IInitializable
{
    protected int currentPage = 0;
    protected readonly int pageSize;
    private bool hasMorePages = true;

    protected PagedListViewModelBase(int pageSize = 10)
    {
        this.pageSize = pageSize;
    }

    public ObservableCollection<TViewModel> Items { get; } = new();
    protected virtual bool CanLoadNextPage => hasMorePages;

    /// <summary>
    /// Initializes the list by ensuring source data exists and loading its first page.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RunCommandAsync(async () =>
        {
            await EnsureDummyData();
            ResetPaging();
            await LoadNextPageCore();
        }, showRefreshing: true);
    }

    /// <summary>
    /// Loads the next available page of list items.
    /// </summary>
    [RelayCommand]
    public async Task LoadNextPage()
    {
        if (IsBusy) return;
        if (!CanLoadNextPage) return;
        await RunCommandAsync(LoadNextPageCore);
    }

    /// <summary>
    /// Reinitializes the list from scratch.
    /// </summary>
    [RelayCommand]
    private Task Refresh() => InitializeAsync();

    /// <summary>
    /// Resets paging state and removes all current items.
    /// </summary>
    protected void ResetPaging()
    {
        currentPage = 0;
        hasMorePages = true;
        Items.Clear();
    }

    /// <summary>
    /// Invoked after a mapped view model is added to <see cref="Items"/>.
    /// </summary>
    /// <param name="vm">The view model that was added.</param>
    protected virtual void OnViewModelAdded(TViewModel vm) { }

    /// <summary>
    /// Ensures any data required to populate the list is available.
    /// </summary>
    protected abstract Task EnsureDummyData();

    /// <summary>
    /// Loads a page of source items.
    /// </summary>
    /// <param name="pageNumber">The zero-based page number to load.</param>
    /// <param name="pageSize">The maximum number of source items to load.</param>
    /// <returns>The source items for the requested page.</returns>
    protected abstract Task<List<TSource>> LoadAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Maps a source item to its list view model.
    /// </summary>
    /// <param name="source">The source item to map.</param>
    /// <returns>The mapped list view model.</returns>
    protected abstract TViewModel MapToViewModel(TSource source);

    /// <summary>
    /// Replaces the current list with a complete, unpaged result set.
    /// </summary>
    /// <param name="sources">The source items to display.</param>
    protected void ReplaceWithFullResultSet(IEnumerable<TSource> sources)
    {
        ResetPaging();
        var list = sources.ToList();
        AddItemsPage(list);
        hasMorePages = false; // full set loaded
    }

    /// <summary>
    /// Replaces the current list with the first page from the normal data source.
    /// </summary>
    protected async Task ReplaceWithFirstPagedAsync()
    {
        ResetPaging();

        var page = await LoadAsync(currentPage, pageSize);
        if (page.Count == 0)
        {
            hasMorePages = false;
            return;
        }

        AddItemsPage(page);
        if (page.Count < pageSize)
            hasMorePages = false;

        currentPage++;
    }

    private void AddItemsPage(List<TSource> sources)
    {
        foreach (var s in sources)
        {
            var vm = MapToViewModel(s);
            Items.Add(vm);
            OnViewModelAdded(vm);
        }
    }

    private async Task LoadNextPageCore()
    {
        if (!hasMorePages) return;
        var page = await LoadAsync(currentPage, pageSize);

        if (page.Count == 0)
        {
            hasMorePages = false;
            return;
        }

        AddItemsPage(page);

        if (page.Count < pageSize)
            hasMorePages = false;

        currentPage++;
    }
}
