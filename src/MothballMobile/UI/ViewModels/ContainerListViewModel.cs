using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Entities.ContainerAggregate;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : BaseViewModel
{
    private ObservableCollection<ContainerViewModel> containers = new();
    private readonly IImagePathResolver imagePaths;
    private readonly Infrastructure.INavigationService nav;
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly int pageSize = 10;
    private int currentPage = 0;
    private List<string> allContainerIds;
    private List<Container> allContainers = new();

    private readonly Infrastructure.DemoDataSeeder? demoSeeder; // optional in debug

    public ContainerListViewModel(IImagePathResolver imagePaths, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav, Infrastructure.DemoDataSeeder? demoSeeder = null)
    {
        this.imagePaths = imagePaths;
        this.inventoryRepository = inventoryRepository;
        this.nav = nav;
        this.demoSeeder = demoSeeder;
        Containers = new ObservableCollection<ContainerViewModel>();
        allContainerIds = new List<string>();
    }

    public ObservableCollection<ContainerViewModel> Containers
    {
        get => containers;
        set => SetProperty(ref containers, value);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RunCommandAsync(async () =>
        {
            // Seed demo data in dev if repo is empty
            if (demoSeeder is not null)
            {
                await demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
                await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: true);
            }

            // Domain repository returns rich aggregates (with ImageItem lists populated)
            allContainers = await inventoryRepository.GetAllContainersAsync();
            allContainerIds = allContainers.Select(c => c.ContainerId.ToString()).ToList();

            // Reset existing state
            currentPage = 0;
            Containers.Clear();

            // Load first page without re-checking the loading flag
            LoadNextPageCore();
        });
    }

    [RelayCommand]
    public void LoadNextPage()
    {
        if (IsBusy) return;
        if (allContainerIds.Count == 0) return;
        LoadNextPageCore();
    }

    private void LoadNextPageCore()
    {
        if (allContainerIds.Count == 0) return;

        int start = currentPage * pageSize;
        if (start >= allContainerIds.Count) return;

        int count = Math.Min(pageSize, allContainerIds.Count - start);
        var pageContainers = allContainers.Skip(start).Take(count).ToList();

        foreach (var container in pageContainers)
        {
            var vm = new ContainerViewModel(container, imagePaths, nav);
            Containers.Add(vm);
            // Kick off image load without blocking the UI thread.
            _ = vm.LoadImageAsync();
        }

        currentPage++;
    }

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => nav.GoToAsync(Infrastructure.NavigationRoutes.AddContainer);
}

// Moved ContainerViewModel to its own file for SRP and clarity.
