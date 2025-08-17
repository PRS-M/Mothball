using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp;
using CoreApp.Services.Implementations;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : ObservableObject
{

    private ObservableCollection<ContainerViewModel> containers = new();
    public ObservableCollection<ContainerViewModel> Containers
    {
        get => containers;
        set => SetProperty(ref containers, value);
    }

    private readonly ContainerJsonHandler _containerJsonHandler;
    private readonly IFileHandler _fileHandler;
    private readonly int _pageSize = 10;
    private int _currentPage = 0;
    private bool _isLoading;
    private List<string> _allContainerFiles;

    public ContainerListViewModel(ContainerJsonHandler containerJsonHandler, IFileHandler fileHandler)
    {
        _containerJsonHandler = containerJsonHandler;
        _fileHandler = fileHandler;
        Containers = new ObservableCollection<ContainerViewModel>();
        _allContainerFiles = new List<string>();
    }

    public async Task InitializeAsync()
    {
        // Get all container JSON files for paging
        var containersPath = Path.Combine(_fileHandler.GetAppDataPath(), Constants.PathToContainers);
        if (!Directory.Exists(containersPath))
            Directory.CreateDirectory(containersPath);

        _allContainerFiles = _fileHandler.EnumerateFiles(containersPath, "*.json").ToList();
        // If no files, create dummy data
        if (_allContainerFiles.Count == 0)
        {
            for (int i = 1; i <= 15; i++)
            {
                var dummy = new Container(
                    uniqueId: Guid.NewGuid().ToString(),
                    name: $"Dummy_Container_{i}",
                    description: $"Description for container {i}",
                    locationDescription: $"Location {i}",
                    photo: new Photo { FileName = string.Empty }
                );

                await _containerJsonHandler.SaveContainerAsync(dummy);
            }
            _allContainerFiles = Directory.EnumerateFiles(containersPath, "*.json").ToList();
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
        var filesToLoad = _allContainerFiles.Skip(_currentPage * _pageSize).Take(_pageSize).ToList();
        foreach (var file in filesToLoad)
        {
            var container = await _containerJsonHandler.LoadContainerFromFileAsync(Path.GetFileName(file));
            var vm = new ContainerViewModel(container, _fileHandler);
            await vm.LoadImageAsync();
            Containers.Add(vm);
        }
        _currentPage++;
        _isLoading = false;
    }

    // Placeholder for future add-container command
}

public class ContainerViewModel : ObservableObject
{
    public Container Container { get; }
    public Dictionary<string, List<string>> ItemIdsByContainerId { get; }
    private readonly IFileHandler _fileHandler;
    private ImageSource _imageSource;
    public ImageSource ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public string Name => Container.Name;
    public string Description => Container.Description;
    public string LocationDescription => Container.LocationDescription;
    // public int ItemCount => Container.ItemIds?.Count ?? 0;

    public ContainerViewModel(Container container, IFileHandler fileHandler)
    {
        Container = container;
        _fileHandler = fileHandler;
        _imageSource = "dotnet_bot.png";
    }

    public async Task LoadImageAsync()
    {
        if (Container.Photo != null && !string.IsNullOrEmpty(Container.Photo.FileName))
        {
            try
            {
                var ms = await _fileHandler.GetImageMemoryStream(Container.Photo.FileName, Constants.PathToContainers);
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
