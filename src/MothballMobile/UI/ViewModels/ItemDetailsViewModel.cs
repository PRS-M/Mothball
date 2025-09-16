using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Entities.ItemAggregate;

namespace MothballMobile.UI.ViewModels;

public partial class ItemDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly Infrastructure.INavigationService nav;
    private readonly IImagePathResolver paths;
    private readonly Infrastructure.IPopupService popup;
    private readonly ImageService imageService;
    private readonly Infrastructure.IRetryService retryService;
    private Item? currentItem;

    [ObservableProperty]
    private string itemId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string? containerId;

    public bool HasContainerRelation => !string.IsNullOrWhiteSpace(this.ContainerId);

    public ObservableCollection<string> ImagePaths { get; } = new();

    public ItemDetailsViewModel(IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav, IImagePathResolver paths, Infrastructure.IPopupService popup, ImageService imageService, Infrastructure.IRetryService retryService)
    {
        this.inventoryRepository = inventoryRepository;
        this.nav = nav;
        this.paths = paths;
        this.popup = popup;
        this.imageService = imageService;
        this.retryService = retryService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(nameof(ItemId), out var val) && val is string id && !string.IsNullOrWhiteSpace(id))
        {
            _ = InitializeAsync(id);
        }
    }

    public async Task InitializeAsync(string itemId)
    {
        await RunCommandAsync(async () =>
        {
            ItemId = itemId;
            ImagePaths.Clear();

            var item = await inventoryRepository.GetItemWithPhotosAsync(itemId);
            if (item is null)
            {
                Name = "Item not found";
                Description = string.Empty;
                ImagePaths.Add(paths.GetFallbackImagePath());
                return;
            }

            currentItem = item;
            Name = item.Name;
            Description = item.Description;

            foreach (var path in paths.GetItemPhotoPaths(item))
                ImagePaths.Add(path);

            // Use repository to find related container, if any
            var container = await inventoryRepository.GetContainerForItemAsync(item.ItemId.ToString());
            if (container is not null)
            {
                ContainerId = container.ContainerId.ToString();
                OnPropertyChanged(nameof(HasContainerRelation));
            }
        });
    }

    [RelayCommand]
    private Task NavigateToContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return Task.CompletedTask;
        return nav.GoToAsync(Infrastructure.NavigationRoutes.ContainerDetails,
            new Dictionary<string, object> { [Infrastructure.NavigationParams.ContainerId] = ContainerId! });
    }

    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId)) return;
        var confirmed = await popup.ConfirmAsync(
            title: "Delete item",
            message: "Are you sure you want to delete this item? This cannot be undone.",
            accept: "Delete",
            cancel: "Cancel");
        if (!confirmed) return;

        await inventoryRepository.DeleteItemAsync(ItemId);
        await nav.GoBackAsync();
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (currentItem is null) return;

        await RunCommandAsync(async () =>
        {
            var captured = await retryService.RetryAsync(
                attempt: async () => (await imageService.CaptureItemPhotoAsync(currentItem)) > 0,
                canceledTitle: "Photo capture canceled",
                canceledMessage: "Please try again or continue without a photo.",
                retryButton: "Retry",
                continueButton: "Continue",
                continueAlertTitle: "No photo",
                continueAlertMessage: "Continuing without a photo.");

            if (captured)
            {
                ImagePaths.Clear();
                foreach (var path in paths.GetItemPhotoPaths(currentItem))
                    ImagePaths.Add(path);
            }
        });
    }
}
