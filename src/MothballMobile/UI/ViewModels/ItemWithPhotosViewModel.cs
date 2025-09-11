using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading.Tasks;

namespace MothballMobile.UI.ViewModels;

public class ItemWithPhotosViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IImagePathResolver _paths;

    public ObservableCollection<string> ImagePaths { get; } = new();

    public ItemWithPhotosViewModel(Item item, IImagePathResolver paths)
    {
        Item = item;
        _paths = paths;
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public Task LoadImagesAsync()
    {
        ImagePaths.Clear();
        if (Item.Photos != null && Item.Photos.Any())
            foreach (var photo in Item.Photos)
                ImagePaths.Add(_paths.GetItemPhotoPath(photo));
        else
            ImagePaths.Add(_paths.GetFallbackImagePath());
        return Task.CompletedTask;
    }
}
