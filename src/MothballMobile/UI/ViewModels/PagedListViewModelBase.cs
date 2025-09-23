using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public abstract partial class PagedListViewModelBase<TSource, TViewModel> : BaseViewModel, IInitializable
{
    protected int currentPage = 0;
    protected readonly int pageSize;

    public ObservableCollection<TViewModel> Items { get; } = new();

    protected PagedListViewModelBase(int pageSize = 10)
    {
        this.pageSize = pageSize;
    }

    public async Task InitializeAsync()
    {
        await RunCommandAsync(async () =>
        {
            await EnsureDummyData();

            currentPage = 0;
            Items.Clear();

            await LoadNextPageCore();
        }, showRefreshing: true);
    }

    [RelayCommand]
    public async Task LoadNextPage()
    {
        if (IsBusy) return;
        await LoadNextPageCore();
    }

    private async Task LoadNextPageCore()
    {
        var page = await LoadAsync(currentPage, pageSize);

        if (page.Count == 0) return;
        foreach (var s in page)
        {
            var vm = MapToViewModel(s);
            Items.Add(vm);
            OnViewModelAdded(vm);
        }

        currentPage++;
    }

    protected virtual void OnViewModelAdded(TViewModel vm) { }
    protected abstract Task EnsureDummyData();
    protected abstract Task<List<TSource>> LoadAsync(int pageNumber, int pageSize);
    protected abstract TViewModel MapToViewModel(TSource source);
}
