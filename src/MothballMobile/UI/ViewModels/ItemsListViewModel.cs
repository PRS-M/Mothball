using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading;
using Infrastructure.Utilities;
using Microsoft.Maui.ApplicationModel;
using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.ViewModels;

public partial class ItemsListViewModel : BaseViewModel, IDisposable, MothballMobile.Infrastructure.IInitializable
{
    private readonly IImagePathResolver paths;
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly Infrastructure.INavigationService nav;

    public ObservableCollection<ItemViewModel> Items { get; } = new();

    private readonly IDebouncer debouncer;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string query = string.Empty;

    public ItemsListViewModel(IImagePathResolver paths, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav, IDebouncer? debouncer = null)
    {
        this.paths = paths;
        this.inventoryRepository = inventoryRepository;
        this.nav = nav;
    this.debouncer = debouncer ?? new Debouncer(300);
    }

    private bool disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing && debouncer is IDisposable d)
        {
            d.Dispose();
        }
        disposed = true;
    }

    public async Task InitializeAsync()
    {
        IsRefreshing = true;
        await RunCommandAsync(async () =>
        {
            await LoadAsync(Query);
        }, showRefreshing: true);
    }

    [RelayCommand]
    private Task RefreshAsync()
    {
        return InitializeAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await RunCommandAsync(async () =>
        {
            await LoadAsync(Query);
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
    private Task NavigateToItemDetailsAsync(Guid itemId)
    {
        var id = itemId.ToString();
        return nav.GoToAsync(Infrastructure.NavigationRoutes.ItemDetails,
            new Dictionary<string, object> { [Infrastructure.NavigationParams.ItemId] = id });
    }

    [RelayCommand]
    private Task NavigateToAddItemAsync()
    {
        return nav.GoToAsync(Infrastructure.NavigationRoutes.AddItem);
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
        debouncer.Debounce(() => MainThread.BeginInvokeOnMainThread(() => _ = SearchAsync()));
    }
    #pragma warning restore IDE0051

}
