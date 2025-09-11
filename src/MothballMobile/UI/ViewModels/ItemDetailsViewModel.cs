using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using Microsoft.Maui.ApplicationModel;
using System.IO;

namespace MothballMobile.UI.ViewModels;

public partial class ItemDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IInventoryDomainRepository _inventoryRepository;
    private readonly Infrastructure.INavigationService _nav;
    private readonly IImagePathResolver _paths;
    private readonly Infrastructure.IPopupService _popup;

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

    public ItemDetailsViewModel(IInventoryDomainRepository inventoryRepository, Infrastructure.INavigationService nav, IImagePathResolver paths, Infrastructure.IPopupService popup)
    {
        _inventoryRepository = inventoryRepository;
        _nav = nav;
        _paths = paths;
        _popup = popup;
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
        ItemId = itemId;
        ImagePaths.Clear();

        var item = await _inventoryRepository.GetItemWithPhotosAsync(itemId);
        if (item is null)
        {
            Name = "Item not found";
            Description = string.Empty;
            ImagePaths.Add(_paths.GetFallbackImagePath());
            return;
        }

        Name = item.Name;
        Description = item.Description;

        if (item.Photos is { Count: > 0 })
        {
            foreach (var p in item.Photos)
                ImagePaths.Add(_paths.GetItemPhotoPath(p));
        }
        else
        {
            ImagePaths.Add(_paths.GetFallbackImagePath());
        }

        // Use repository to find related container, if any
        var container = await _inventoryRepository.GetContainerForItemAsync(item.ItemId.ToString());
        if (container is not null)
        {
            ContainerId = container.ContainerId.ToString();
            OnPropertyChanged(nameof(HasContainerRelation));
        }
    }

    [RelayCommand]
    private Task NavigateToContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(ContainerId)) return Task.CompletedTask;
        return _nav.GoToAsync("ContainerDetails", new Dictionary<string, object> { ["ContainerId"] = ContainerId! });
    }

    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId)) return;
        var confirmed = await _popup.ConfirmAsync(
            title: "Delete item",
            message: "Are you sure you want to delete this item? This cannot be undone.",
            accept: "Delete",
            cancel: "Cancel");
        if (!confirmed) return;

        await _inventoryRepository.DeleteItemAsync(ItemId);
        await _nav.GoBackAsync();
    }
}
