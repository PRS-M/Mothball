using System.Collections.ObjectModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using Microsoft.Maui.Controls;

namespace MothballMobile.UI.ViewModels;

public partial class ContainerDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IInventoryDomainRepository _inventoryRepository;
    private readonly IFileHandler _fileHandler;

    [ObservableProperty]
    private string containerId = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string itemCount = string.Empty;

    public ObservableCollection<ImageSource> ContainerImageSources { get; } = new();
    public ObservableCollection<ItemWithPhotosViewModel> Items { get; } = new();

    public ContainerDetailsViewModel(IInventoryDomainRepository inventoryRepository, IFileHandler fileHandler)
    {
        _inventoryRepository = inventoryRepository;
        _fileHandler = fileHandler;
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
        ContainerImageSources.Clear();

        var result = await _inventoryRepository.GetContainerWithItemsAndPhotosAsync(containerId);
        if (result is null)
        {
            Name = "Container not found";
            Notes = string.Empty;
            ItemCount = string.Empty;
            ContainerImageSources.Add("dotnet_bot.png");
            return;
        }

        var (container, items) = result.Value;
        Name = container.Name;
        Notes = container.Notes;
        ItemCount = $"Items stored: {container.ItemCount}";

        // Load container photos (all, as a small carousel)
        if (container.Photos?.Count > 0)
        {
            foreach (var photo in container.Photos)
            {
                try
                {
                    var ms = await _fileHandler.GetImageMemoryStream(photo.FileName, Constants.PathToContainerPhotos);
                    var bytes = ms.ToArray();
                    await ms.DisposeAsync();
                    ContainerImageSources.Add(ImageSource.FromStream(() => new MemoryStream(bytes)));
                }
                catch
                {
                    ContainerImageSources.Add("dotnet_bot.png");
                }
            }
        }
        else
        {
            ContainerImageSources.Add("dotnet_bot.png");
        }

        // Map items and load their images (carousel per item)
        foreach (var item in items)
        {
            var itemVm = new ItemWithPhotosViewModel(item, _fileHandler);
            Items.Add(itemVm);
            _ = itemVm.LoadImagesAsync();
        }
    }
}
