using CoreApp.Domain.Entities.ContainerAggregate;
using Microsoft.Extensions.Logging.Abstractions;

namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public sealed class AssociateItemWithContainerCoordinator : IDisposable
{
    private readonly IImagePathResolver imagePaths;
    private readonly IContainerAssociationQueryHandler associationQueries;
    private readonly IApplicationSettings applicationSettings;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly IDebouncer debouncer;

    public AssociateItemWithContainerCoordinator(
        IImagePathResolver imagePaths,
        IContainerAssociationQueryHandler associationQueries,
        IApplicationSettings applicationSettings,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null)
    {
        this.imagePaths = imagePaths;
        this.associationQueries = associationQueries;
        this.applicationSettings = applicationSettings;
        this.backgroundTasks = backgroundTasks;
        this.debouncer = debouncer ?? new Debouncer(300, NullLogger<Debouncer>.Instance);
    }

    public Task<List<Container>> LoadPageAsync(int pageNumber, int pageSize)
        => associationQueries.QueryContainersAsync(pageNumber, pageSize);

    public Task<List<Container>> SearchAsync(string searchQuery)
        => associationQueries.QueryContainersAsync(searchQuery);

    public SelectableContainerViewModel CreateContainerViewModel(
        Container container,
        Func<Guid, Task> associateAsync)
    {
        var viewModel = new SelectableContainerViewModel(
            container,
            imagePaths,
            associateAsync,
            applicationSettings.IsAdvancedMode);
        viewModel.LoadImagesAsync().FireAndForget(backgroundTasks, "Load selectable container images");
        return viewModel;
    }

    public void DebounceSearch(Func<Task> searchAsync)
        => debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(searchAsync))
            .FireAndForget(backgroundTasks, "Search association containers");

    public void Dispose()
    {
        if (debouncer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
