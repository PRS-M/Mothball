using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp;
using CoreApp.Services.Implementations;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;
using CoreApp.Models;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : ObservableObject
{

    private ObservableCollection<ContainerViewModel> containers = new();
    public ObservableCollection<ContainerViewModel> Containers
    {
        get => containers;
        set => SetProperty(ref containers, value);
    }

    private readonly InventoryJsonHandler _inventoryHandler;
    private readonly IFileHandler _fileHandler;
    private readonly int _pageSize = 10;
    private int _currentPage = 0;
    private bool _isLoading;
    private List<string> _allContainerIds;

    public ContainerListViewModel(InventoryJsonHandler inventoryHandler, IFileHandler fileHandler)
    {
        _inventoryHandler = inventoryHandler;
        _fileHandler = fileHandler;
        Containers = new ObservableCollection<ContainerViewModel>();
        _allContainerIds = new List<string>();
    }

    public async Task InitializeAsync()
    {
        // Load aggregate and prepare paging by IDs
        var inventory = await _inventoryHandler.LoadAsync();
        _allContainerIds = inventory.Containers.Keys.ToList();

        // If empty, create dummy data via aggregate
        if (_allContainerIds.Count == 0)
        {
            for (int i = 1; i <= 15; i++)
            {
                var dummy = new Container(
                    uniqueId: Guid.NewGuid().ToString(),
                    name: $"Dummy_Container_{i}",
                    locationDescription: $"Location {i}",
                    description: $"Description for container {i}",
                    photos: [new Photo(string.Empty)]
                );
                inventory.AddContainer(dummy);
            }
            await _inventoryHandler.SaveAsync(inventory);
            _allContainerIds = inventory.Containers.Keys.ToList();
        }

        _currentPage = 0;
        Containers.Clear();
        await LoadNextPageAsync();
    }

    [RelayCommand]
    public async Task LoadNextPageAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        var inventory = await _inventoryHandler.LoadAsync();
        var idsToLoad = _allContainerIds.Skip(_currentPage * _pageSize).Take(_pageSize).ToList();
        foreach (var id in idsToLoad)
        {
            if (!inventory.Containers.TryGetValue(id, out var container)) continue;
            int count = inventory.ItemIdsByContainerId.TryGetValue(container.UniqueId, out var list) ? list.Count : 0;
            var vm = new ContainerViewModel(container, _fileHandler, count);
            await vm.LoadImageAsync();
            Containers.Add(vm);
        }
        _currentPage++;
        _isLoading = false;
    }

    // Optional: placeholder navigate command
    [RelayCommand]
    private Task NavigateAsync(ContainerViewModel? vm)
    {
        return Task.CompletedTask;
    }
}

public class ContainerViewModel : ObservableObject
{
    public Container Container { get; }
    public Dictionary<string, List<string>> ItemIdsByContainerId { get; } = new();
    private readonly IFileHandler _fileHandler;
    private ImageSource _imageSource;
    private readonly int _itemCount;
    public ImageSource ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public string Name => Container.Name;
    public string Description => Container.Description;
    public string LocationDescription => Container.LocationDescription;
    public int ItemCount => _itemCount;

    public ContainerViewModel(Container container, IFileHandler fileHandler, int itemCount)
    {
        Container = container;
        _fileHandler = fileHandler;
        _itemCount = itemCount;
        _imageSource = "dotnet_bot.png";
    }

    public async Task LoadImageAsync()
    {
        if (Container.Photos != null && Container.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
            try
            {
                var photoFolder = Path.Combine(Constants.PathToItemPhotos, Container.UniqueId);

                // TODO: Multiple photos handling.
                var ms = await _fileHandler.GetImageMemoryStream(Container.Photos[0].FileName, photoFolder);
                ImageSource = ImageSource.FromStream(() => ms);
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
