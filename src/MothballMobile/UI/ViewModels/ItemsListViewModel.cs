using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Utilities;
using CoreApp.Interfaces;
using System.IO;
using System.Threading;
using Microsoft.Maui.ApplicationModel;

namespace MothballMobile.UI.ViewModels;

public partial class ItemsListViewModel : ObservableObject
{
    private readonly IFileHandler _fileHandler;
    private readonly IInventoryDomainRepository _inventoryRepository;
    private readonly Infrastructure.INavigationService _nav;

    public ObservableCollection<ItemViewModel> Items { get; } = new();

    private bool _isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string query = string.Empty;

    private CancellationTokenSource? _searchCts;

    public ItemsListViewModel(IFileHandler fileHandler, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav)
    {
        _fileHandler = fileHandler;
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

    private async Task LoadAsync(string? query)
    {
        Items.Clear();
        var items = string.IsNullOrWhiteSpace(query)
            ? await _inventoryRepository.GetAllItemsWithPhotosAsync()
            : await _inventoryRepository.GetItemsWithPhotosAsync(query);

        foreach (var item in items)
        {
            var vm = new ItemViewModel(item, _fileHandler, _nav);
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

    public partial class ItemViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IFileHandler _fileHandler;
        private readonly Infrastructure.INavigationService _nav;
        private string _imagePath;

        public ItemViewModel(Item item, IFileHandler fileHandler, Infrastructure.INavigationService nav)
    {
        Item = item;
        _fileHandler = fileHandler;
            _nav = nav;
            _imagePath = "dotnet_bot.png";
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        public Task LoadImageAsync()
    {
        if (Item.Photos != null && Item.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
                var path = Path.Combine(_fileHandler.GetAppDataPath(), Constants.PathToItemPhotos, Item.Photos[0].FileName);
                ImagePath = path;
        }
        else
        {
                ImagePath = "dotnet_bot.png";
        }
            return Task.CompletedTask;
    }

        [RelayCommand]
        private Task NavigateToItemDetailsAsync()
        {
            return _nav.GoToAsync("ItemDetails", new Dictionary<string, object>
            {
                ["ItemId"] = Item.ItemId.ToString()
            });
        }
}

public class ItemWithPhotosViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IFileHandler _fileHandler;

        public ObservableCollection<string> ImagePaths { get; } = new();

    public ItemWithPhotosViewModel(Item item, IFileHandler fileHandler)
    {
        Item = item;
        _fileHandler = fileHandler;
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

        public Task LoadImagesAsync()
    {
            ImagePaths.Clear();
        if (Item.Photos != null && Item.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
                foreach (var photo in Item.Photos)
                {
                    var path = Path.Combine(_fileHandler.GetAppDataPath(), Constants.PathToItemPhotos, photo.FileName);
                    ImagePaths.Add(path);
                }
        }
        else
        {
                ImagePaths.Add("dotnet_bot.png");
        }
            return Task.CompletedTask;
    }
}
