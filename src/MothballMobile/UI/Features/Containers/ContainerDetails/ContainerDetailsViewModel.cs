using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.Shared;
using CoreApp.Application.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;


public partial class ContainerDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable, IDisposable, IContainerDetailsHeader
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
    private string barcodeValue = string.Empty;

    [ObservableProperty]
    private string barcodeSymbology = string.Empty;

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

    [ObservableProperty]
    private bool isLoadingItems;

    public bool IsViewingNotes => !IsEditingNotes;
    public bool HasBarcode => !string.IsNullOrWhiteSpace(BarcodeValue);
    public bool ShowQuantityManagement => applicationSettings.IsAdvancedMode;
    public string DisplayNotes => string.IsNullOrWhiteSpace(Notes) ? "No description." : Notes;
    public string ItemsStoredText => LocalizationManager.Current.Format("Items stored (Total): {0}", TotalItemCount);
    public string ItemTypesStoredText => LocalizationManager.Current.Format("Item types stored: {0}", ItemTypesCount);

    partial void OnTotalItemCountChanged(int value)
        => OnPropertyChanged(nameof(ItemsStoredText));

    partial void OnItemTypesCountChanged(int value)
        => OnPropertyChanged(nameof(ItemTypesStoredText));

    partial void OnNotesChanged(string value)
        => OnPropertyChanged(nameof(DisplayNotes));

    partial void OnBarcodeValueChanged(string value)
        => OnPropertyChanged(nameof(HasBarcode));

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
    }

    partial void OnSearchQueryChanged(string value)
    {
        debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(PerformSearchAsync))
            .FireAndForget(backgroundTasks, "Search container items");
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
        IsLoadingItems = false;
        ContainerImagePaths.Clear();

        var summary = await itemCoordinator.LoadSummaryAsync(containerId, this);
        if (summary is null)
        {
            currentContainer = null;
            Name = "Container not found";
            Notes = string.Empty;
            NotesDraft = string.Empty;
            BarcodeValue = string.Empty;
            BarcodeSymbology = string.Empty;
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
        BarcodeValue = currentContainer.Barcode?.Value ?? string.Empty;
        BarcodeSymbology = currentContainer.Barcode?.Symbology.ToString() ?? string.Empty;
        IsEditingNotes = false;
        ItemTypesCount = summary.ItemTypesCount;
        TotalItemCount = ShowQuantityManagement ? summary.TotalItemCount : summary.ItemTypesCount;

        // Load container photos (all, as a small carousel)
        ReplaceWith(ContainerImagePaths, paths.GetContainerPhotoPaths(currentContainer));

        // Publish the header and image paths before starting the item query. Once the
        // query yields, MAUI can render and size the container carousel independently.
        IsItemListEmpty = false;
        IsLoadingItems = true;
        try
        {
            if (await itemCoordinator.ReloadAsync(
                    ContainerId,
                    currentContainer,
                    searchTerm: null,
                    ShowQuantityManagement))
            {
                IsItemListEmpty = itemCoordinator.IsEmpty;
            }
        }
        finally
        {
            IsLoadingItems = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        if (IsLoadingItems)
        {
            return;
        }

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
        IsLoadingItems = true;

        try
        {
            if (currentContainer is not null && await itemCoordinator.ReloadAsync(
                    ContainerId,
                    currentContainer,
                    searchTerm,
                    ShowQuantityManagement))
            {
                IsItemListEmpty = itemCoordinator.IsEmpty;
            }
        }
        finally
        {
            IsLoadingItems = false;
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

        await popup.ConfirmAndRunAsync(popupDefinitions.DeleteContainer(), async () =>
        {
            await deleteContainerHandler.DeleteAsync(ContainerId);
            await nav.GoBackAsync();
        });
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
    private Task DeletePhotoAsync()
    {
        if (currentContainer is null) return Task.CompletedTask;

        return DeleteSelectedPhotoAsync(
            hasPhotos: currentContainer.Photos.Count > 0,
            noPhotosPopup: popupDefinitions.NoContainerPhotos(),
            pickerDefinition: popupDefinitions.ContainerPhotoDeletePicker(currentContainer.Photos),
            deleteAsync: imageId => imageService.DeleteContainerPhotoAsync(currentContainer, imageId),
            targetPaths: ContainerImagePaths,
            refreshedPaths: () => paths.GetContainerPhotoPaths(currentContainer));
    }

    [RelayCommand]
    private Task NavigateToAddExistingItemAsync()
    {
        if (!Guid.TryParse(ContainerId, out var containerId)) return Task.CompletedTask;

        return nav.GoToAsync(NavigationRoutes.AddExistingItemToContainer,
            new Infrastructure.Navigation.AddExistingItemToContainerNavigationRequest(containerId));
    }

    [RelayCommand]
    private Task NavigateToAddNewItemAsync()
    {
        if (!Guid.TryParse(ContainerId, out var containerId)) return Task.CompletedTask;

        return nav.GoToAsync(NavigationRoutes.AddItem,
            new Infrastructure.Navigation.AddItemNavigationRequest(containerId));
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
