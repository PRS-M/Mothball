using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;
using MothballMobile.Infrastructure;
using Infrastructure.Services;

namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public partial class AssociateItemWithContainerViewModel : PagedListViewModelBase<Container, SelectableContainerViewModel>, IQueryAttributable
{
    private readonly IImagePathResolver imagePaths;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly Infrastructure.INavigationService nav;
    private readonly DemoDataSeeder? demoSeeder;

    private string? itemId;

    public AssociateItemWithContainerViewModel(
        IImagePathResolver imagePaths,
        IInventoryQueryRepository inventoryQueries,
        IInventoryCommandRepository inventoryCommands,
        Infrastructure.INavigationService nav,
        DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.inventoryQueries = inventoryQueries;
        this.inventoryCommands = inventoryCommands;
        this.nav = nav;
        this.demoSeeder = demoSeeder;
    }

    public ObservableCollection<SelectableContainerViewModel> Containers => Items;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParams.ItemId, out var value) && value is string id && !string.IsNullOrWhiteSpace(id))
        {
            itemId = id;
        }
    }

    protected override async Task EnsureDummyData()
    {
        if (demoSeeder is not null)
        {
            await demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
        }
    }

    protected override Task<List<Container>> LoadAsync(int pageNumber, int pageSize)
        => inventoryQueries.QueryContainersAsync(
            new ContainerListSpecification(
                Filter: ContainerQueryFilter.All,
                PageNumber: pageNumber,
                PageSize: pageSize));

    protected override SelectableContainerViewModel MapToViewModel(Container source)
        => new(source, imagePaths, AssociateWithContainerAsync);

    protected override void OnViewModelAdded(SelectableContainerViewModel vm)
        => _ = vm.LoadImagesAsync();

    private async Task AssociateWithContainerAsync(Guid containerId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        if (!Guid.TryParse(itemId, out var parsedItemId)) return;

        await RunCommandAsync(async () =>
        {
            await inventoryCommands.InsertItemContainerRelation(parsedItemId, containerId, quantity: 1);
            await nav.GoBackAsync();
        });
    }
}
