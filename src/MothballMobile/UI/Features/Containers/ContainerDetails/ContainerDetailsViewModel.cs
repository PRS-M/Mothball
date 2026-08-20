using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Application.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ContainerDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable, IDisposable
{
    private readonly IDeleteContainerCommandHandler deleteContainerHandler;
    private readonly IUpdateContainerNotesCommandHandler updateContainerNotesHandler;
    private readonly IDebouncer debouncer;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly ContainerDetailsItemsCoordinator itemCoordinator;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private Container? currentContainer;

    [ObservableProperty]
    private string containerId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string notesDraft = string.Empty;

    [ObservableProperty]
    private bool isEditingNotes;

    [ObservableProperty]
    private int totalItemCount = 0;

    [ObservableProperty]
    private int itemTypesCount = 0;

    public ObservableCollection<string> ContainerImagePaths { get; } = new();
    public ObservableCollection<ItemWithPhotosViewModel> Items => itemCoordinator.Items;
    public ObservableCollection<object> Rows => itemCoordinator.Rows;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool isItemListEmpty = true;

    public bool IsViewingNotes => !IsEditingNotes;
    public bool ShowQuantityManagement => applicationSettings.IsAdvancedMode;
    public string DisplayNotes => string.IsNullOrWhiteSpace(Notes) ? "No description." : Notes;
    public string ItemsStoredText => $"Items stored (Total): {TotalItemCount}";
    public string ItemTypesStoredText => $"Item types stored: {ItemTypesCount}";

    partial void OnTotalItemCountChanged(int value)
        => OnPropertyChanged(nameof(ItemsStoredText));

    partial void OnItemTypesCountChanged(int value)
        => OnPropertyChanged(nameof(ItemTypesStoredText));

    partial void OnNotesChanged(string value)
        => OnPropertyChanged(nameof(DisplayNotes));

    public ContainerDetailsViewModel(
        IDeleteContainerCommandHandler deleteContainerHandler,
        IUpdateContainerNotesCommandHandler updateContainerNotesHandler,
        IImagePathResolver paths,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ImageService imageService,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IPhotoBackgroundOperationTracker photoBackgroundOperationTracker,
        ContainerDetailsItemsCoordinator itemCoordinator,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null)
        : base(paths, imageService, popup, popupDefinitions, photoBackgroundOperationTracker)
    {
        this.deleteContainerHandler = deleteContainerHandler;
        this.updateContainerNotesHandler = updateContainerNotesHandler;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.itemCoordinator = itemCoordinator;
        this.backgroundTasks = backgroundTasks;
        this.debouncer = debouncer ?? new Debouncer(250, NullLogger<Debouncer>.Instance);
        itemCoordinator.Reset(this);

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
    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue(nameof(ContainerId), out var value) && value is string id && !string.IsNullOrWhiteSpace(id))
        {
            ContainerId = id;
        }
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        if (itemCoordinator.TryConsumeSkipNextInitialization())
        {
            return Task.CompletedTask;
        }

        return InitializeAsync(ContainerId);
    }

    /// <summary>
    /// Loads container details, photos, and initial item-list state for the specified container.
    /// </summary>
    /// <param name="containerId">The identifier of the container to load.</param>
    public async Task InitializeAsync(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId)) return;

        ContainerId = containerId;
        SearchQuery = string.Empty;

        IsItemListEmpty = true;
        ContainerImagePaths.Clear();

        var summary = await itemCoordinator.InitializeAsync(containerId, this, ShowQuantityManagement);
        if (summary is null)
        {
            currentContainer = null;
            Name = "Container not found";
            Notes = string.Empty;
            NotesDraft = string.Empty;
            IsEditingNotes = false;
            TotalItemCount = 0;
            ItemTypesCount = 0;
            ContainerImagePaths.Add(paths.GetFallbackImagePath());
            IsItemListEmpty = true;
            return;
        }

        currentContainer = summary.Container;
        Name = currentContainer.Name;
        Notes = currentContainer.Notes;
        NotesDraft = currentContainer.Notes;
        IsEditingNotes = false;
        ItemTypesCount = summary.ItemTypesCount;
        TotalItemCount = ShowQuantityManagement ? summary.TotalItemCount : summary.ItemTypesCount;

        // Load container photos (all, as a small carousel)
        ReplaceWith(ContainerImagePaths, paths.GetContainerPhotoPaths(currentContainer));
        IsItemListEmpty = itemCoordinator.IsEmpty;
    }

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        // Use RunCommandAsync to prevent concurrent loads and manage busy state
        await RunCommandAsync(async () =>
        {
            if (currentContainer is null)
            {
                return;
            }

            await itemCoordinator.LoadMoreAsync(ContainerId, currentContainer, ShowQuantityManagement);
            IsItemListEmpty = itemCoordinator.IsEmpty;
        });
    }

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return;

        var searchTerm = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
        IsItemListEmpty = false;

        if (currentContainer is not null && await itemCoordinator.ReloadAsync(
                ContainerId,
                currentContainer,
                searchTerm,
                ShowQuantityManagement))
        {
            IsItemListEmpty = itemCoordinator.IsEmpty;
        }
    }

    [RelayCommand]
    private async Task ApplySearch()
    {
        await PerformSearchAsync();
    }

    [RelayCommand]
    private void EditNotes()
    {
        NotesDraft = Notes;
        IsEditingNotes = true;
    }

    [RelayCommand]
    private async Task SaveNotesAsync()
    {
        if (currentContainer is null)
        {
            return;
        }

        var updatedNotes = NotesDraft?.Trim() ?? string.Empty;
        await RunCommandAsync(async () =>
        {
            await updateContainerNotesHandler.UpdateAsync(currentContainer, updatedNotes);
            Notes = currentContainer.Notes;
            NotesDraft = currentContainer.Notes;
            IsEditingNotes = false;
        });
    }

    partial void OnIsEditingNotesChanged(bool value)
        => OnPropertyChanged(nameof(IsViewingNotes));

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

    /// <inheritdoc />
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
