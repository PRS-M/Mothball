using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Entities.ContainerAggregate;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : PagedListViewModelBase<Container, ContainerViewModel>
{
    private readonly IImagePathResolver imagePaths;
    private readonly Infrastructure.INavigationService nav;
    private readonly IInventoryDomainRepository inventoryRepository;

    private readonly Infrastructure.DemoDataSeeder? demoSeeder; // optional in debug

    public ContainerListViewModel(IImagePathResolver imagePaths, IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav, Infrastructure.DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.inventoryRepository = inventoryRepository;
        this.nav = nav;
        this.demoSeeder = demoSeeder;
    }

    public ObservableCollection<ContainerViewModel> Containers => Items;

    protected override async Task LoadAllAsync()
    {
        if (demoSeeder is not null)
        {
            await demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
            await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: true);
        }
        var allContainers = await inventoryRepository.GetAllContainersAsync();
        allItems.Clear();
        allItems.AddRange(allContainers);
    }

    protected override ContainerViewModel MapToViewModel(Container source)
        => new ContainerViewModel(source, imagePaths, nav);

    protected override void OnViewModelAdded(ContainerViewModel vm)
        => _ = vm.LoadImageAsync();

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => nav.GoToAsync(Infrastructure.NavigationRoutes.AddContainer);
}

// Moved ContainerViewModel to its own file for SRP and clarity.
