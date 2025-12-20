using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MothballMobile.Infrastructure;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Entities.ContainerAggregate;

namespace MothballMobile.UI.ViewModels;

public partial class ContainerDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable, IDisposable
{
    private readonly IInventoryDomainRepository inventoryRepository;
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
    private string itemCount = string.Empty;

    public ObservableCollection<string> ContainerImagePaths { get; } = new();
    public ObservableCollection<ItemWithPhotosViewModel> Items { get; } = new();

    private readonly List<ItemWithPhotosViewModel> allItems = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

    public ContainerDetailsViewModel(
        IInventoryDomainRepository inventoryRepository,
        IImagePathResolver paths,
        IPopupService popup,
        ImageService imageService,
        IRetryService retryService,
        INavigationService nav,
        IDebouncer? debouncer = null)
        : base(paths, imageService, retryService)
    {
        this.inventoryRepository = inventoryRepository;
        this.popup = popup;
        this.nav = nav;
        this.debouncer = debouncer ?? new Debouncer(250);
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

        Items.Clear();
        ContainerImagePaths.Clear();

        var result = await inventoryRepository.GetContainerWithItemsAndPhotosAsync(containerId);
        if (result is null)
        {
            Name = "Container not found";
            Notes = string.Empty;
            ItemCount = string.Empty;
            ContainerImagePaths.Add(paths.GetFallbackImagePath());
            return;
        }

        var (container, items) = result.Value;
        currentContainer = container;
        Name = container.Name;
        Notes = container.Notes;
        ItemCount = $"Items stored: {container.ItemCount}";

        // Load container photos (all, as a small carousel)
        ReplaceWith(ContainerImagePaths, paths.GetContainerPhotoPaths(container));

        // Map items and load their images (carousel per item)
        allItems.Clear();
        foreach (var item in items)
        {
            var itemVm = new ItemWithPhotosViewModel(item, paths);
            allItems.Add(itemVm);
            _ = itemVm.LoadImagesAsync();
        }

        ApplyFilter();
    }

    [RelayCommand]
    private void ApplySearch()
    {
        ApplyFilter();
    }

    partial void OnSearchQueryChanged(string value)
    {
        // Debounce user typing to avoid excessive filtering on fast input
        debouncer.Debounce(() => MainThread.BeginInvokeOnMainThread(ApplyFilter));
    }

    private void ApplyFilter()
    {
        Items.Clear();
        IEnumerable<ItemWithPhotosViewModel> source = allItems;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            source = source.Where(vm => vm.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        foreach (var vm in source)
        {
            Items.Add(vm);
        }
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

        await inventoryRepository.DeleteContainerAsync(ContainerId);
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
