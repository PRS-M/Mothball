using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public abstract partial class PagedListViewModelBase<TSource, TViewModel> : BaseViewModel, IInitializable
{
    protected readonly List<TSource> allItems = new();
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
            await LoadAllAsync();
            currentPage = 0;
            Items.Clear();
            LoadNextPageCore();
        }, showRefreshing: true);
    }

    [RelayCommand]
    public void LoadNextPage()
    {
        if (IsBusy) return;
        if (allItems.Count == 0) return;
        LoadNextPageCore();
    }

    private void LoadNextPageCore()
    {
        if (allItems.Count == 0) return;
        int start = currentPage * pageSize;
        if (start >= allItems.Count) return;
        int count = Math.Min(pageSize, allItems.Count - start);
        var page = allItems.Skip(start).Take(count).ToList();
        foreach (var s in page)
        {
            var vm = MapToViewModel(s);
            Items.Add(vm);
            OnViewModelAdded(vm);
        }
        currentPage++;
    }

    protected virtual void OnViewModelAdded(TViewModel vm) { }

    protected abstract Task LoadAllAsync();
    protected abstract TViewModel MapToViewModel(TSource source);
}
