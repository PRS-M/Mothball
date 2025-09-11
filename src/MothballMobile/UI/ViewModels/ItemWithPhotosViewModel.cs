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
        foreach (var path in _paths.GetItemPhotoPaths(Item))
            ImagePaths.Add(path);
        return Task.CompletedTask;
    }
}
