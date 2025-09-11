using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using System.IO;
using System.Threading.Tasks;

namespace MothballMobile.UI.ViewModels;

public class ItemWithPhotosViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IFileHandler _fileHandler;

    public ObservableCollection<string> ImagePaths { get; } = new();

    public ItemWithPhotosViewModel(Item item, IFileHandler fileHandler)
    {
        Item = item;
        _fileHandler = fileHandler;
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public Task LoadImagesAsync()
    {
        ImagePaths.Clear();
        if (Item.Photos != null && Item.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
            foreach (var photo in Item.Photos)
            {
                var path = Path.Combine(_fileHandler.GetAppDataPath(), Constants.PathToItemPhotos, photo.FileName);
                ImagePaths.Add(path);
            }
        }
        else
        {
            ImagePaths.Add("dotnet_bot.png");
        }
        return Task.CompletedTask;
    }
}
