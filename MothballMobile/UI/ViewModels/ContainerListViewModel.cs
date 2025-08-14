using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp;

namespace MothballMobile.UI.ViewModels;

using System.Threading.Tasks;
using System.Linq;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using CoreApp.Services.Implementations;
using MothballMobile.Core.Services;
using CoreApp.Utilities;

public partial class ContainerListViewModel : ObservableObject
{

    private ObservableCollection<ContainerViewModel> containers = new();
    public ObservableCollection<ContainerViewModel> Containers
    {
        get => containers;
        set => SetProperty(ref containers, value);
    }

    private readonly ContainerJsonHandler _containerJsonHandler;
    private readonly IMobileFileHandler _mobileFileHandler;
    private readonly int _pageSize = 10;
    private int _currentPage = 0;
    private bool _isLoading;
    private List<string> _allContainerFiles;

    public ContainerListViewModel(ContainerJsonHandler containerJsonHandler, IMobileFileHandler mobileFileHandler)
    {
        _containerJsonHandler = containerJsonHandler;
        _mobileFileHandler = mobileFileHandler;
        Containers = new ObservableCollection<ContainerViewModel>();
        _allContainerFiles = new List<string>();
    }

    public async Task InitializeAsync()
    {
        // Get all container JSON files for paging
        var containersPath = Path.Combine(_mobileFileHandler.GetAppDataPath(), Constants.PathToContainers);
        if (!Directory.Exists(containersPath))
            Directory.CreateDirectory(containersPath);
        _allContainerFiles = Directory.EnumerateFiles(containersPath, "*.json").ToList();
        // If no files, create dummy data
        if (_allContainerFiles.Count == 0)
        {
            for (int i = 1; i <= 15; i++)
            {
                var dummy = new Container(
                    id: i,
                    uniqueId: Guid.NewGuid().ToString(),
                    name: $"Dummy_Container_{i}",
                    description: $"Description for container {i}",
                    locationDescription: $"Location {i}",
                    items: [],
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
            var vm = new ContainerViewModel(container, _mobileFileHandler);
            await vm.LoadImageAsync();
            Containers.Add(vm);
        }
        _currentPage++;
        _isLoading = false;
    }

    // [RelayCommand]
    // public void AddContainer(Container container)
    // {
    //     if (container == null)
    //         throw new ArgumentNullException(nameof(container), "Container cannot be null.");
    //     var vm = new ContainerViewModel(container, _mobileFileHandler);
    //     Containers.Add(vm);
    // }
}

public class ContainerViewModel : ObservableObject
{
    public Container Container { get; }
    private readonly IMobileFileHandler _mobileFileHandler;
    private ImageSource _imageSource;
    public ImageSource ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public string Name => Container.Name;
    public string Description => Container.Description;
    public string LocationDescription => Container.LocationDescription;
    public int ItemCount => Container.Items?.Count ?? 0;

    public ContainerViewModel(Container container, IMobileFileHandler mobileFileHandler)
    {
        Container = container;
        _mobileFileHandler = mobileFileHandler;
        _imageSource = "dotnet_bot.png";
    }

    public async Task LoadImageAsync()
    {
        if (Container.Photo != null && !string.IsNullOrEmpty(Container.Photo.FileName))
        {
            try
            {
                ImageSource = await _mobileFileHandler.GetImageSourceAsync(Container.Photo.FileName, Constants.PathToContainers);
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
