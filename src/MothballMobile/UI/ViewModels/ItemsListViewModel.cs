using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading;
using Microsoft.Maui.ApplicationModel;
using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.ViewModels;

public partial class ItemsListViewModel : ObservableObject
{
    private readonly IImagePathResolver _paths;
    private readonly IInventoryDomainRepository _inventoryRepository;
    private readonly Infrastructure.INavigationService _nav;

    public ObservableCollection<ItemViewModel> Items { get; } = new();

    private bool _isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string query = string.Empty;

    private CancellationTokenSource? _searchCts;

    public ItemsListViewModel(IImagePathResolver paths, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav)
    {
        _paths = paths;
        _inventoryRepository = inventoryRepository;
        _nav = nav;
    }

    public async Task InitializeAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        IsRefreshing = true;
        try
        {
            await LoadAsync(Query);
        }
        finally
        {
            _isLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync()
    {
        return InitializeAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            await LoadAsync(Query);
        }
        finally
        {
            _isLoading = false;
        }
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
        return _nav.GoToAsync("ItemDetails", new Dictionary<string, object> { ["ItemId"] = id });
    }

    [RelayCommand]
    private Task NavigateToAddItemAsync()
    {
        return _nav.GoToAsync("AddItem");
    }

    private async Task LoadAsync(string? query)
    {
        Items.Clear();
        var items = string.IsNullOrWhiteSpace(query)
            ? await _inventoryRepository.GetAllItemsWithPhotosAsync()
            : await _inventoryRepository.GetItemsWithPhotosAsync(query);

        foreach (var item in items)
        {
            var vm = new ItemViewModel(item, _paths, _nav);
            Items.Add(vm);
            _ = vm.LoadImageAsync();
        }
    }

    // MVVM Toolkit hook: raised when Query changes
    // Source generator hook from [ObservableProperty]
    // The CommunityToolkit.Mvvm generator invokes this partial method
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by MVVM Toolkit source generator")]
    partial void OnQueryChanged(string value)
    {
        // Debounce user typing
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;
                await MainThread.InvokeOnMainThreadAsync(() => SearchAsync());
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }, token);
    }
    #pragma warning restore IDE0051

}
