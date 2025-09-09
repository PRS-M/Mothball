using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Utilities;
using CoreApp.Interfaces;
using CoreApp.Entities.ContainerAggregate;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : ObservableObject
{
    private ObservableCollection<ContainerViewModel> containers = new();
    private readonly IFileHandler _fileHandler;
    private readonly Infrastructure.INavigationService _nav;
    private readonly IInventoryDomainRepository _inventoryRepository;
    private readonly int _pageSize = 10;
    private int _currentPage = 0;
    private bool _isLoading;
    private List<string> _allContainerIds;
    private List<Container> _allContainers = new();

    private readonly Infrastructure.DemoDataSeeder? _demoSeeder; // optional in debug

    public ContainerListViewModel(IFileHandler fileHandler, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav, Infrastructure.DemoDataSeeder? demoSeeder = null)
    {
        _fileHandler = fileHandler;
        _inventoryRepository = inventoryRepository;
        _nav = nav;
        _demoSeeder = demoSeeder;
        Containers = new ObservableCollection<ContainerViewModel>();
        _allContainerIds = new List<string>();
    }

    public ObservableCollection<ContainerViewModel> Containers
    {
        get => containers;
        set => SetProperty(ref containers, value);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            // Seed demo data in dev if repo is empty
            if (_demoSeeder is not null)
            {
                await _demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
            }

            // Domain repository returns rich aggregates (with ImageItem lists populated)
            _allContainers = await _inventoryRepository.GetAllContainersAsync();
            _allContainerIds = _allContainers.Select(c => c.ContainerId.ToString()).ToList();

            // Reset existing state
            _currentPage = 0;
            Containers.Clear();

            // Load first page without re-checking the loading flag
            LoadNextPageCore();
        }
        finally
        {
            _isLoading = false;
        }
    }

    [RelayCommand]
    public void LoadNextPage()
    {
        if (_isLoading) return;
        if (_allContainerIds.Count == 0) return;

        _isLoading = true;
        try
        {
            LoadNextPageCore();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LoadNextPageCore()
    {
        if (_allContainerIds.Count == 0) return;

        int start = _currentPage * _pageSize;
        if (start >= _allContainerIds.Count) return;

        int count = Math.Min(_pageSize, _allContainerIds.Count - start);
        var pageContainers = _allContainers.Skip(start).Take(count).ToList();

        foreach (var container in pageContainers)
        {
            var vm = new ContainerViewModel(container, _fileHandler);
            Containers.Add(vm);
            // Kick off image load without blocking the UI thread.
            _ = vm.LoadImageAsync();
        }

        _currentPage++;
    }

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => _nav.GoToAsync("AddContainer");
}

public partial class ContainerViewModel : ObservableObject
{
    public Container Container { get; }
    public Dictionary<string, List<string>> ItemIdsByContainerId { get; } = new();
    private readonly IFileHandler _fileHandler;
    private ObservableCollection<string> _imagePaths;

    public ObservableCollection<string> ImagePaths
    {
        get => _imagePaths;
        set => SetProperty(ref _imagePaths, value);
    }

    public string Name => Container.Name;
    public string Notes => Container.Notes;
    public string ItemCount => $"Items stored: {Container.ItemCount}";

    public ContainerViewModel(Container container, IFileHandler fileHandler)
    {
        Container = container;
        _fileHandler = fileHandler;
        _imagePaths = new ObservableCollection<string>();
    }

    public Task LoadImageAsync()
    {
        if (Container.Photos != null && Container.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
            try
            {
                // Provide a file path the View can convert to an ImageSource
                var path = Path.Combine(_fileHandler.GetAppDataPath(), Constants.PathToContainerPhotos, Container.Photos[0].FileName);
                ImagePaths.Add(path);
            }
            catch
            {
                ImagePaths.Add("dotnet_bot.png");
            }
        }
        else
        {
            ImagePaths.Add("dotnet_bot.png");
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task NavigateAsync()
    {
        var id = Container.ContainerId.ToString();
        // Resolve from the service provider associated with the App
        var nav = Application.Current?.Handler?.MauiContext?.Services?.GetService<Infrastructure.INavigationService>();
        return nav?.GoToAsync("ContainerDetails", new Dictionary<string, object> { ["ContainerId"] = id })
               ?? Task.CompletedTask;
    }
}
