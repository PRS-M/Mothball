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
    private readonly IImagePathResolver paths;
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly Infrastructure.INavigationService nav;

    public ObservableCollection<ItemViewModel> Items { get; } = new();

    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string query = string.Empty;

    private CancellationTokenSource? searchCts;

    public ItemsListViewModel(IImagePathResolver paths, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav)
    {
        this.paths = paths;
        this.inventoryRepository = inventoryRepository;
        this.nav = nav;
    }

    public async Task InitializeAsync()
    {
        if (isLoading) return;
        isLoading = true;
        IsRefreshing = true;
        try
        {
            await LoadAsync(Query);
        }
        finally
        {
            isLoading = false;
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
        if (isLoading) return;
        isLoading = true;
        try
        {
            await LoadAsync(Query);
        }
        finally
        {
            isLoading = false;
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
        return nav.GoToAsync("ItemDetails", new Dictionary<string, object> { ["ItemId"] = id });
    }

    [RelayCommand]
    private Task NavigateToAddItemAsync()
    {
        return nav.GoToAsync("AddItem");
    }

    private async Task LoadAsync(string? query)
    {
        Items.Clear();
        var items = string.IsNullOrWhiteSpace(query)
            ? await inventoryRepository.GetAllItemsWithPhotosAsync()
            : await inventoryRepository.GetItemsWithPhotosAsync(query);

        foreach (var item in items)
        {
            var vm = new ItemViewModel(item, paths, nav);
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
    searchCts?.Cancel();
    searchCts?.Dispose();
    searchCts = new CancellationTokenSource();
    var token = searchCts.Token;

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
