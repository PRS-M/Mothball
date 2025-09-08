using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Utilities;
using CoreApp.Interfaces;

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

    // Provide a passthrough for bindings expecting the older name
    // Generated command (for RefreshAsync) is RefreshCommand; expose both.
    public ICommand RefreshAsyncCommand => RefreshCommand;
}

public class ItemViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IFileHandler _fileHandler;
    private ImageSource _imageSource;

    public ItemViewModel(Item item, IFileHandler fileHandler)
    {
        Item = item;
        _fileHandler = fileHandler;
        _imageSource = "dotnet_bot.png";
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public ImageSource ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public async Task LoadImageAsync()
    {
        if (Item.Photos != null && Item.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
            try
            {
                var ms = await _fileHandler.GetImageMemoryStream(Item.Photos[0].FileName, Constants.PathToItemPhotos);
                var bytes = ms.ToArray();
                await ms.DisposeAsync();
                ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            {
                ImageSource = "dotnet_bot.png";
            }
        }
        else
        {
            ImageSource = "dotnet_bot.png";
        }
    }
}

public class ItemWithPhotosViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IFileHandler _fileHandler;

    public ObservableCollection<ImageSource> ImageSources { get; } = new();

    public ItemWithPhotosViewModel(Item item, IFileHandler fileHandler)
    {
        Item = item;
        _fileHandler = fileHandler;
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public async Task LoadImagesAsync()
    {
        ImageSources.Clear();
        if (Item.Photos != null && Item.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
            foreach (var photo in Item.Photos)
            {
                try
                {
                    var ms = await _fileHandler.GetImageMemoryStream(photo.FileName, Constants.PathToItemPhotos);
                    var bytes = ms.ToArray();
                    await ms.DisposeAsync();
                    ImageSources.Add(ImageSource.FromStream(() => new MemoryStream(bytes)));
                }
                catch
                {
                    ImageSources.Add("dotnet_bot.png");
                }
            }
        }
        else
        {
            ImageSources.Add("dotnet_bot.png");
        }
    }
}
