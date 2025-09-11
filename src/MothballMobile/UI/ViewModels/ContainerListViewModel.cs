using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Entities.ContainerAggregate;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : ObservableObject
{
    private ObservableCollection<ContainerViewModel> containers = new();
    private readonly IImagePathResolver _imagePaths;
    private readonly Infrastructure.INavigationService _nav;
    private readonly IInventoryDomainRepository _inventoryRepository;
    private readonly int _pageSize = 10;
    private int _currentPage = 0;
    private bool _isLoading;
    private List<string> _allContainerIds;
    private List<Container> _allContainers = new();

    private readonly Infrastructure.DemoDataSeeder? _demoSeeder; // optional in debug

    public ContainerListViewModel(IImagePathResolver imagePaths, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav, Infrastructure.DemoDataSeeder? demoSeeder = null)
    {
        _imagePaths = imagePaths;
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
                await _demoSeeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: true);
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
            var vm = new ContainerViewModel(container, _imagePaths);
            Containers.Add(vm);
            // Kick off image load without blocking the UI thread.
            _ = vm.LoadImageAsync();
        }

        _currentPage++;
    }

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => _nav.GoToAsync("AddContainer");
}

// Moved ContainerViewModel to its own file for SRP and clarity.
