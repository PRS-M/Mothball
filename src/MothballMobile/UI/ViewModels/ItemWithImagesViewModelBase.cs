using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;

namespace MothballMobile.UI.ViewModels;

public abstract class ItemWithImagesViewModelBase : ObservableObject
{
    protected ItemWithImagesViewModelBase(Item item, IImagePathResolver paths)
    {
        Item = item;
        this.paths = paths;
    }

    private readonly IImagePathResolver paths;

    public Item Item { get; }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public ObservableCollection<string> ImagePaths { get; } = new();

    protected Task LoadItemImagesAsync(bool clearFirst = true)
    {
        if (clearFirst)
        {
            ImagePaths.Clear();
        }

        foreach (var imagePath in paths.GetItemPhotoPaths(Item))
        {
            ImagePaths.Add(imagePath);
        }

        return Task.CompletedTask;
    }
}
