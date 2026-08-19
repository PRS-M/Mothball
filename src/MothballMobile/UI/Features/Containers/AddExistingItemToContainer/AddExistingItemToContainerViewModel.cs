using CoreApp.Entities.InventoryAggregate;
﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Contracts;

namespace MothballMobile.UI.Features.Containers.AddExistingItemToContainer;

public partial class AddExistingItemToContainerViewModel : PagedListViewModelBase<InventorySnapshot, UnassignedItemViewModel>, IQueryAttributable
{
    private readonly IContainerAssociationQueryHandler associationQueries;
    private readonly IAssignItemToContainerCommandHandler assignItemToContainer;
    private readonly IImagePathResolver paths;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly IBackgroundTaskObserver backgroundTasks;

    [ObservableProperty]
    private string containerId = string.Empty;

    public AddExistingItemToContainerViewModel(
        IContainerAssociationQueryHandler associationQueries,
        IAssignItemToContainerCommandHandler assignItemToContainer,
        IImagePathResolver paths,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IBackgroundTaskObserver backgroundTasks)
    {
        this.associationQueries = associationQueries;
        this.assignItemToContainer = assignItemToContainer;
        this.paths = paths;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.backgroundTasks = backgroundTasks;
    }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue(NavigationParams.ContainerId, out var value) && value is string id)
        {
            ContainerId = id;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => InitializeAsync();

    /// <inheritdoc />
    protected override Task EnsureDummyData() => Task.CompletedTask;

    /// <inheritdoc />
    protected override UnassignedItemViewModel MapToViewModel(InventorySnapshot source)
        => new(source, paths, AssignAsync, applicationSettings.IsAdvancedMode);

    /// <inheritdoc />
    protected override void OnViewModelAdded(UnassignedItemViewModel vm)
        => vm.LoadImagesAsync().FireAndForget(backgroundTasks, "Load unassigned item images");

    /// <inheritdoc />
    protected override Task<List<InventorySnapshot>> LoadAsync(int pageNumber, int pageSize)
    {
        Guid? excludedContainerId = Guid.TryParse(ContainerId, out var parsedContainerId)
            ? parsedContainerId
            : null;

        return associationQueries.QueryUnassignedItemsAsync(
            pageNumber,
            pageSize,
            excludedContainerId);
    }

    private async Task AssignAsync(Guid itemId)
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return;
        if (!Guid.TryParse(ContainerId, out var cid)) return;

        await RunCommandAsync(async () =>
        {
            await assignItemToContainer.AssignAsync(itemId, cid);
            await nav.GoBackAsync();
        });
    }
}
