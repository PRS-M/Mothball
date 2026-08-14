using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;
using Infrastructure.Services;

namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public partial class AssociateItemWithContainerViewModel : PagedListViewModelBase<Container, SelectableContainerViewModel>, IQueryAttributable
{
    private readonly IImagePathResolver imagePaths;
    private readonly IContainerAssociationQueryHandler associationQueries;
    private readonly IAssignItemToContainerCommandHandler assignItemToContainer;
    private readonly Infrastructure.INavigationService nav;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly DemoDataSeeder? demoSeeder;

    private string? itemId;

    public AssociateItemWithContainerViewModel(
        IImagePathResolver imagePaths,
        IContainerAssociationQueryHandler associationQueries,
        IAssignItemToContainerCommandHandler assignItemToContainer,
        Infrastructure.INavigationService nav,
        IBackgroundTaskObserver backgroundTasks,
        DemoDataSeeder? demoSeeder = null)
        : base(pageSize: 10)
    {
        this.imagePaths = imagePaths;
        this.associationQueries = associationQueries;
        this.assignItemToContainer = assignItemToContainer;
        this.nav = nav;
        this.backgroundTasks = backgroundTasks;
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
        => associationQueries.QueryContainersAsync(pageNumber, pageSize);

    protected override SelectableContainerViewModel MapToViewModel(Container source)
        => new(source, imagePaths, AssociateWithContainerAsync);

    protected override void OnViewModelAdded(SelectableContainerViewModel vm)
        => vm.LoadImagesAsync().FireAndForget(backgroundTasks, "Load selectable container images");

    private async Task AssociateWithContainerAsync(Guid containerId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        if (!Guid.TryParse(itemId, out var parsedItemId)) return;

        await RunCommandAsync(async () =>
        {
            await assignItemToContainer.AssignAsync(parsedItemId, containerId);
            await nav.GoBackAsync();
        });
    }
}
