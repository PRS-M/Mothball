using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MothballMobile.Infrastructure;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using MothballMobile.Infrastructure.Popups;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ContainerDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable, IDisposable
{
    private readonly IContainerDetailsQueryHandler containerDetailsQueries;
    private readonly IDeleteContainerCommandHandler deleteContainerHandler;
    private readonly IDebouncer debouncer;
    private readonly INavigationService nav;
    private readonly ContainerItemPagingController itemPaging;
    private readonly ContainerDetailsItemRowsViewModel itemRows;
    private readonly IContainerItemQuantityService quantityService;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private Container? currentContainer;

    [ObservableProperty]
    private string containerId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private int totalItemCount = 0;

    public ObservableCollection<string> ContainerImagePaths { get; } = new();
    public ObservableCollection<ItemWithPhotosViewModel> Items { get; } = new();
    public ObservableCollection<object> Rows { get; } = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool isItemListEmpty = true;

    private const int PageSize = 5;

    public ContainerDetailsViewModel(
        IContainerDetailsQueryHandler containerDetailsQueries,
        IDeleteContainerCommandHandler deleteContainerHandler,
        IImagePathResolver paths,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ImageService imageService,
        INavigationService nav,
        IPhotoBackgroundOperationTracker photoBackgroundOperationTracker,
        IContainerItemQuantityService quantityService,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null)
        : base(paths, imageService, popup, popupDefinitions, photoBackgroundOperationTracker)
    {
        this.containerDetailsQueries = containerDetailsQueries;
        this.deleteContainerHandler = deleteContainerHandler;
        this.nav = nav;
        this.quantityService = quantityService;
        this.backgroundTasks = backgroundTasks;
        this.debouncer = debouncer ?? new Debouncer(250, NullLogger<Debouncer>.Instance);
        itemPaging = new ContainerItemPagingController(containerDetailsQueries, PageSize);
        itemRows = new ContainerDetailsItemRowsViewModel(this, Items, Rows);

        // Debounce search query changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchQuery))
            {
                this.debouncer?.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(PerformSearchAsync))
                    .FireAndForget(backgroundTasks, "Search container items");
            }
        };
    }

    // Let Shell pass query params directly to the ViewModel.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue(nameof(ContainerId), out var value) && value is string id && !string.IsNullOrWhiteSpace(id))
        {
            ContainerId = id;
        }
    }

    public Task InitializeAsync()
        => InitializeAsync(ContainerId);

    public async Task InitializeAsync(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId)) return;

        ContainerId = containerId;
        itemPaging.Reset();
        SearchQuery = string.Empty;

        itemRows.Reset();
        IsItemListEmpty = true;
        ContainerImagePaths.Clear();

        var details = await containerDetailsQueries.GetDetailsAsync(containerId);
        if (details is null)
        {
            currentContainer = null;
            Name = "Container not found";
            Notes = string.Empty;
            TotalItemCount = 0;
            ContainerImagePaths.Add(paths.GetFallbackImagePath());
            itemPaging.MarkComplete();
            IsItemListEmpty = true;
            return;
        }

        var container = details.Container;
        currentContainer = container;
        Name = container.Name;
        Notes = container.Notes;
        TotalItemCount = details.TotalItemCount;

        // Load container photos (all, as a small carousel)
        ReplaceWith(ContainerImagePaths, paths.GetContainerPhotoPaths(container));

        await ReloadItemsAsync(searchTerm: null);
    }

    private void AddItemsToCollection(IEnumerable<ContainerItemInventoryEntry> items)
    {
        itemRows.Append(
            items,
            entry =>
            {
                var itemVm = new ItemWithPhotosViewModel(
                    entry,
                    currentContainer?.ContainerId ?? Guid.Empty,
                    paths,
                    nav,
                    popup,
                    popupDefinitions,
                    ContainerId,
                    SaveItemQuantityAsync);
                itemVm.LoadImagesAsync().FireAndForget(backgroundTasks, "Load container item images");
                return itemVm;
            });

        IsItemListEmpty = itemRows.IsEmpty;
        OnPropertyChanged(nameof(Items));
    }

    private async Task SaveItemQuantityAsync(Guid itemId, int quantity)
    {
        if (currentContainer is null)
        {
            return;
        }

        var result = await quantityService.SaveQuantityAsync(currentContainer, itemId, quantity);
        TotalItemCount = result.TotalItemCount;
        var searchTerm = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
        await ReloadItemsAsync(searchTerm);
    }

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        // Use RunCommandAsync to prevent concurrent loads and manage busy state
        await RunCommandAsync(async () =>
        {
            var page = await itemPaging.LoadMoreAsync(ContainerId);
            if (page.IsStale)
            {
                return;
            }

            if (page.Items.Count > 0)
            {
                AddItemsToCollection(page.Items);
            }
        });
    }

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return;

        var searchTerm = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
        await ReloadItemsAsync(searchTerm);
    }

    private async Task ReloadItemsAsync(string? searchTerm)
    {
        itemRows.ClearItems();
        IsItemListEmpty = false;

        var page = await itemPaging.ReloadAsync(ContainerId, searchTerm);
        if (page.IsStale)
        {
            return;
        }

        AddItemsToCollection(page.Items);
    }

    [RelayCommand]
    private async Task ApplySearch()
    {
        await PerformSearchAsync();
    }

    [RelayCommand]
    private async Task DeleteContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return;

        var confirmed = await popup.ConfirmAsync(popupDefinitions.DeleteContainer());

        if (!confirmed) return;

        await deleteContainerHandler.DeleteAsync(ContainerId);
        await nav.GoBackAsync();
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (currentContainer is null) return;
        if (IsPhotoCaptureInProgress) return;

        var source = await SelectPhotoSourceAsync();
        if (source is null)
        {
            return;
        }

        // Run in background so persistence can finish even if the user leaves this view.
        CaptureTrackedPhotoAsync(
            operationName: "Saving container photo",
            captureAsync: progress => imageService.CaptureContainerPhotoAsync(currentContainer, progress, source.Value),
            targetPaths: ContainerImagePaths,
            refreshedPaths: () => paths.GetContainerPhotoPaths(currentContainer),
            shouldRefresh: () => !disposed).FireAndForget(backgroundTasks, "Save container photo");
    }

    [RelayCommand]
    private async Task DeletePhotoAsync()
    {
        if (currentContainer is null) return;
        if (currentContainer.Photos.Count == 0)
        {
            await popup.ShowAlertAsync(popupDefinitions.NoContainerPhotos());
            return;
        }

        var selectedPhoto = await SelectPhotoAsync(popupDefinitions.ContainerPhotoDeletePicker(currentContainer.Photos));
        if (selectedPhoto is null)
        {
            return;
        }

        var confirmed = await popup.ConfirmAsync(popupDefinitions.DeletePhoto());

        if (!confirmed)
        {
            return;
        }

        await RunCommandAsync(async () =>
        {
            var deleted = await imageService.DeleteContainerPhotoAsync(currentContainer, selectedPhoto.ImageId);
            if (deleted)
            {
                ReplaceWith(ContainerImagePaths, paths.GetContainerPhotoPaths(currentContainer));
            }
        });
    }

    [RelayCommand]
    private Task NavigateToAddExistingItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return Task.CompletedTask;

        return nav.GoToAsync(NavigationRoutes.AddExistingItemToContainer,
            new Dictionary<string, object> { [NavigationParams.ContainerId] = ContainerId });
    }

    [RelayCommand]
    private Task NavigateToAddNewItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return Task.CompletedTask;

        return nav.GoToAsync(NavigationRoutes.AddItem,
            new Dictionary<string, object> { [NavigationParams.ContainerId] = ContainerId });
    }

    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing && debouncer is IDisposable d)
        {
            d.Dispose();
        }
        disposed = true;
    }
}
