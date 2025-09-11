using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;

namespace MothballMobile.UI.ViewModels;

public partial class ContainerDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IInventoryDomainRepository _inventoryRepository;
    private readonly IImagePathResolver _paths;
    private CancellationTokenSource? _searchCts;
    private readonly Infrastructure.INavigationService? _nav;
    private readonly Infrastructure.IPopupService _popup;

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

    private readonly List<ItemWithPhotosViewModel> _allItems = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

    public ContainerDetailsViewModel(IInventoryDomainRepository inventoryRepository, IImagePathResolver paths, Infrastructure.IPopupService popup, Infrastructure.INavigationService? nav = null)
    {
        _inventoryRepository = inventoryRepository;
        _paths = paths;
        _popup = popup;
        _nav = nav;
    }

    // Let Shell pass query params directly to the ViewModel.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue(nameof(ContainerId), out var value) && value is string id && !string.IsNullOrWhiteSpace(id))
        {
            // fire-and-forget; navigation flow shouldn't be blocked
            _ = InitializeAsync(id);
        }
    }

    public async Task InitializeAsync(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId)) return;

        ContainerId = containerId;

        Items.Clear();
        ContainerImagePaths.Clear();

        var result = await _inventoryRepository.GetContainerWithItemsAndPhotosAsync(containerId);
        if (result is null)
        {
            Name = "Container not found";
            Notes = string.Empty;
            ItemCount = string.Empty;
            ContainerImagePaths.Add(_paths.GetFallbackImagePath());
            return;
        }

        var (container, items) = result.Value;
        Name = container.Name;
        Notes = container.Notes;
        ItemCount = $"Items stored: {container.ItemCount}";

        // Load container photos (all, as a small carousel)
        if (container.Photos?.Count > 0)
            foreach (var photo in container.Photos)
                ContainerImagePaths.Add(_paths.GetContainerPhotoPath(photo));
        else
            ContainerImagePaths.Add(_paths.GetFallbackImagePath());

        // Map items and load their images (carousel per item)
        _allItems.Clear();
        foreach (var item in items)
        {
            var itemVm = new ItemWithPhotosViewModel(item, _paths);
            _allItems.Add(itemVm);
            _ = itemVm.LoadImagesAsync();
        }

        ApplyFilter();
    }

    [RelayCommand]
    private void ApplySearch()
    {
        ApplyFilter();
    }

    // Called automatically by MVVM Toolkit when SearchQuery changes
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by MVVM Toolkit source generator")]
    partial void OnSearchQueryChanged(string value)
    {
        // Debounce user typing to avoid excessive filtering on fast input
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (token.IsCancellationRequested) return;
                await MainThread.InvokeOnMainThreadAsync(ApplyFilter);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }, token);
    }

    private void ApplyFilter()
    {
        Items.Clear();
        IEnumerable<ItemWithPhotosViewModel> source = _allItems;
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
        var confirmed = await _popup.ConfirmAsync(
            title: "Delete container",
            message: "Delete this container? Items inside will not be deleted, only the relation.",
            accept: "Delete",
            cancel: "Cancel");
        if (!confirmed) return;

        await _inventoryRepository.DeleteContainerAsync(ContainerId);
        if (_nav is not null)
        {
            await _nav.GoBackAsync();
        }
    }
}
