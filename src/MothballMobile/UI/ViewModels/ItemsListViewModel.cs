using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Utilities;
using CoreApp.Interfaces;
using System.IO;

namespace MothballMobile.UI.ViewModels;

public partial class ItemsListViewModel : ObservableObject
{
    private readonly IFileHandler _fileHandler;
    private readonly IInventoryDomainRepository _inventoryRepository;

    public ObservableCollection<ItemViewModel> Items { get; } = new();

    private bool _isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    public ItemsListViewModel(IFileHandler fileHandler, IInventoryDomainRepository inventoryRepository)
    {
        _fileHandler = fileHandler;
        _inventoryRepository = inventoryRepository;
    }

    public async Task InitializeAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        IsRefreshing = true;
        try
        {
            Items.Clear();
            var items = await _inventoryRepository.GetAllItemsWithPhotosAsync();
            foreach (var item in items)
            {
                var vm = new ItemViewModel(item, _fileHandler);
                Items.Add(vm);
                _ = vm.LoadImageAsync();
            }
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

}

    public class ItemViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IFileHandler _fileHandler;
        private string _imagePath;

        public ItemViewModel(Item item, IFileHandler fileHandler)
    {
        Item = item;
        _fileHandler = fileHandler;
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
