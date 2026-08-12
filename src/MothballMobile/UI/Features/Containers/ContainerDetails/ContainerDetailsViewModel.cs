using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MothballMobile.Infrastructure;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Specifications;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ContainerDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable, IDisposable
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IDebouncer debouncer;
    private readonly INavigationService nav;
    private readonly IPopupService popup;
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

    [ObservableProperty]
    private string searchQuery = string.Empty;

    private const int PageSize = 5;
    private int currentPage = 0;
    private bool hasMoreItems = true;
    private bool isSearchActive = false;

    public ContainerDetailsViewModel(
        IInventoryQueryRepository inventoryQueries,
        IInventoryCommandRepository inventoryCommands,
        IImagePathResolver paths,
        IPopupService popup,
        ImageService imageService,
        IRetryService retryService,
        INavigationService nav,
        IDebouncer? debouncer = null)
        : base(paths, imageService, retryService)
    {
        this.inventoryQueries = inventoryQueries;
        this.inventoryCommands = inventoryCommands;
        this.popup = popup;
        this.nav = nav;
        this.debouncer = debouncer ?? new Debouncer(250);

        // Debounce search query changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchQuery))
            {
                this.debouncer?.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(PerformSearchAsync)).FireAndForget();
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
        currentPage = 0;
        hasMoreItems = true;
        isSearchActive = false;
        SearchQuery = string.Empty;

        Items.Clear();
        ContainerImagePaths.Clear();

        var container = await inventoryQueries.GetContainerAsync(containerId);
        if (container is null)
        {
            Name = "Container not found";
            Notes = string.Empty;
            TotalItemCount = 0;
            ContainerImagePaths.Add(paths.GetFallbackImagePath());
            hasMoreItems = false;
            return;
        }

        currentContainer = container;
        Name = container.Name;
        Notes = container.Notes;

        // Get the total count from repository (sums all quantities, not just this page)
        TotalItemCount = await inventoryQueries.GetItemCountInContainerAsync(containerId);

        // Load container photos (all, as a small carousel)
        ReplaceWith(ContainerImagePaths, paths.GetContainerPhotoPaths(container));

        var items = await inventoryQueries.QueryContainerItemsWithPhotosAsync(
            new ContainerItemsSpecification(
                ContainerId: containerId,
                PageNumber: currentPage,
                PageSize: PageSize));

        // Map items and load their images (carousel per item)
        AddItemsToCollectionAsync(items, clearExisting: false);

        // Check if we have more items to load
        hasMoreItems = items.Count == PageSize;
    }

    private void AddItemsToCollectionAsync(List<Item> items, bool clearExisting = false)
    {
        if (clearExisting)
        {
            Items.Clear();
        }

        foreach (var item in items)
        {
            var quantity = currentContainer?.Items.FirstOrDefault(x => x.ItemId == item.ItemId)?.Quantity ?? 0;
            var itemVm = new ItemWithPhotosViewModel(item, quantity, paths, nav, ContainerId);
            Items.Add(itemVm);
            itemVm.LoadImagesAsync().FireAndForget();
        }

        // Force collection update notification to recalculate RemainingItemsThreshold
        OnPropertyChanged(nameof(Items));
    }

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        // Use RunCommandAsync to prevent concurrent loads and manage busy state
        await RunCommandAsync(async () =>
        {
            // Guard against attempting to load when no more items exist
            if (!hasMoreItems || string.IsNullOrWhiteSpace(ContainerId)) return;

            currentPage++;
            var items = await inventoryQueries.QueryContainerItemsWithPhotosAsync(
                new ContainerItemsSpecification(
                    ContainerId: ContainerId,
                    SearchTerm: isSearchActive ? SearchQuery : null,
                    PageNumber: currentPage,
                    PageSize: PageSize));

            // Only add items if we got any
            if (items.Count > 0)
            {
                AddItemsToCollectionAsync(items, clearExisting: false);
            }

            // Check if there are more items to load
            // If we got fewer items than the page size, we've reached the end
            hasMoreItems = items.Count == PageSize;
        });
    }

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return;

        currentPage = 0;

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Clear search - reload all items from beginning
            isSearchActive = false;
            await InitializeAsync(ContainerId);
        }
        else
        {
            // Perform search
            isSearchActive = true;
            var searchResults = await inventoryQueries.QueryContainerItemsWithPhotosAsync(
                new ContainerItemsSpecification(
                    ContainerId: ContainerId,
                    SearchTerm: SearchQuery,
                    PageNumber: currentPage,
                    PageSize: PageSize));
            AddItemsToCollectionAsync(searchResults, clearExisting: true);
            hasMoreItems = searchResults.Count == PageSize;
        }
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

        var confirmed = await popup.ConfirmAsync(
            title: "Delete container",
            message: "Delete this container? Items inside will not be deleted, only the relation.",
            accept: "Delete",
            cancel: "Cancel");

        if (!confirmed) return;

        await inventoryCommands.DeleteContainerAsync(ContainerId);
        await nav.GoBackAsync();
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (currentContainer is null) return;

        await RunCommandAsync(async () =>
        {
            var captured = await CaptureWithDefaultRetryAsync(
                attempt: async () => (await imageService.CaptureContainerPhotoAsync(currentContainer)) > 0);

            if (captured)
            {
                ReplaceWith(ContainerImagePaths, paths.GetContainerPhotoPaths(currentContainer));
            }
        });
    }

    [RelayCommand]
    private async Task DeletePhotoAsync()
    {
        if (currentContainer is null) return;
        if (currentContainer.Photos.Count == 0)
        {
            await popup.ShowAlertAsync("No photos", "This container does not have any photos to delete.");
            return;
        }

        var selectedPhoto = await SelectPhotoAsync(currentContainer.Photos, "Choose container photo to delete");
        if (selectedPhoto is null)
        {
            return;
        }

        var confirmed = await popup.ConfirmAsync(
            title: "Delete photo",
            message: "Delete the selected photo?",
            accept: "Delete",
            cancel: "Cancel");

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

    private async Task<ImageItem?> SelectPhotoAsync(IReadOnlyList<ImageItem> photos, string title)
    {
        var optionToPhoto = photos
            .Select((photo, index) => new { Option = $"Photo {index + 1}", Photo = photo })
            .ToList();

        var selected = await popup.SelectOptionAsync(title, "Cancel", optionToPhoto.Select(x => x.Option).ToArray());
        if (string.IsNullOrWhiteSpace(selected))
        {
            return null;
        }

        return optionToPhoto.FirstOrDefault(x => string.Equals(x.Option, selected, StringComparison.Ordinal))?.Photo;
    }

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
