using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MothballMobile.Infrastructure;
using System.Linq;

namespace MothballMobile.UI.ViewModels;

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

    public async Task InitializeAsync()
    {
        await RunCommandAsync(async () =>
        {
            await EnsureDummyData();
            ResetPaging();
            await LoadNextPageCore();
        }, showRefreshing: true);
    }

    [RelayCommand]
    public async Task LoadNextPage()
    {
        if (IsBusy) return;
        if (!CanLoadNextPage) return;
        await LoadNextPageCore();
    }

    protected void ResetPaging()
    {
        currentPage = 0;
        hasMorePages = true;
        Items.Clear();
    }

    protected virtual void OnViewModelAdded(TViewModel vm) { }
    protected abstract Task EnsureDummyData();
    protected abstract Task<List<TSource>> LoadAsync(int pageNumber, int pageSize);
    protected abstract TViewModel MapToViewModel(TSource source);

    // Utility for full replacements (e.g. search result sets)
    protected void ReplaceWithFullResultSet(IEnumerable<TSource> sources)
    {
        ResetPaging();
        var list = sources.ToList();
        AddItemsPage(list);
        hasMorePages = false; // full set loaded
    }

    // Utility if caller wants to restart normal paging (e.g. clearing search)
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
