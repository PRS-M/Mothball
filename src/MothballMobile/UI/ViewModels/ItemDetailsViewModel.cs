using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Entities.ItemAggregate;
using MothballMobile.Infrastructure;
using CoreApp.Services;

namespace MothballMobile.UI.ViewModels;

public partial class ItemDetailsViewModel : PhotoDetailsViewModelBase, IQueryAttributable, IInitializable
{
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly INavigationService nav;
    private readonly IPopupService popup;
    private Item? currentItem;

    [ObservableProperty]
    private string itemId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string? containerId;

    public bool HasNoContainerRelation => string.IsNullOrWhiteSpace(this.ContainerId);
    public bool HasContainerRelation => !HasNoContainerRelation;

    public ObservableCollection<string> ImagePaths { get; } = new();

    public ItemDetailsViewModel(IInventoryDomainRepository inventoryRepository, INavigationService nav, IImagePathResolver paths, IPopupService popup, ImageService imageService, IRetryService retryService)
        : base(paths, imageService, retryService)
    {
        this.inventoryRepository = inventoryRepository;
        this.nav = nav;
        this.popup = popup;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(nameof(ItemId), out var val) && val is string id && !string.IsNullOrWhiteSpace(id))
        {
            ItemId = id;
        }
    }

    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            return Task.CompletedTask;
        }

        return InitializeAsync(ItemId);
    }

    public async Task InitializeAsync(string itemId)
    {
        await RunCommandAsync(async () =>
        {
            ItemId = itemId;
            ImagePaths.Clear();
            ContainerId = null;
            OnPropertyChanged(nameof(HasContainerRelation));
            OnPropertyChanged(nameof(HasNoContainerRelation));

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

            ReplaceWith(ImagePaths, paths.GetItemPhotoPaths(item));

            // Use repository to find related container, if any
            var container = await inventoryRepository.GetContainerForItemAsync(item.ItemId.ToString());
            ContainerId = container?.ContainerId.ToString();
            OnPropertyChanged(nameof(HasContainerRelation));
            OnPropertyChanged(nameof(HasNoContainerRelation));
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
    private Task NavigateToAssociateWithContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId)) return Task.CompletedTask;

        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.AssociateItemWithContainer,
            new Dictionary<string, object> { [NavigationParams.ItemId] = ItemId });
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
            var captured = await CaptureWithDefaultRetryAsync(
                attempt: async () => (await imageService.CaptureItemPhotoAsync(currentItem)) > 0);

            if (captured)
            {
                ReplaceWith(ImagePaths, paths.GetItemPhotoPaths(currentItem));
            }
        });
    }
}
