using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Entities.ContainerAggregate;
using MothballMobile.Infrastructure;
using Infrastructure.Services;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : PagedListViewModelBase<Container, ContainerViewModel>
{
    private readonly IImagePathResolver imagePaths;
    private readonly INavigationService nav;
    private readonly IInventoryDomainRepository inventoryRepository;

    private readonly DemoDataSeeder? demoSeeder; // optional in debug

    public ContainerListViewModel(IImagePathResolver imagePaths, IInventoryDomainRepository inventoryRepository, INavigationService nav, DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.inventoryRepository = inventoryRepository;
        this.nav = nav;
        this.demoSeeder = demoSeeder;
    }

    public ObservableCollection<ContainerViewModel> Containers => Items;

    protected override async Task EnsureDummyData()
    {
        if (demoSeeder is not null)
        {
            await demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
            await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: true);
        }
    }

    protected override async Task<List<Container>> LoadAsync(int pageNumber, int pageSize)
        => await inventoryRepository.GetAllContainersAsync(pageNumber, pageSize);

    protected override ContainerViewModel MapToViewModel(Container source)
        => new ContainerViewModel(source, imagePaths, nav);

    protected override void OnViewModelAdded(ContainerViewModel vm)
        => _ = vm.LoadImageAsync();

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => nav.GoToAsync(NavigationRoutes.AddContainer);
}
