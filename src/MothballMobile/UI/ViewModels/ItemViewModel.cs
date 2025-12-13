using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MothballMobile.UI.ViewModels;

public partial class ItemViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IImagePathResolver paths;
    private readonly Infrastructure.INavigationService nav;
    private ObservableCollection<string> imagePaths;

    public ItemViewModel(Item item, IImagePathResolver paths, Infrastructure.INavigationService nav)
    {
        Item = item;
        this.paths = paths;
        this.nav = nav;
        this.imagePaths = new ObservableCollection<string>();
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public ObservableCollection<string> ImagePaths
    {
        get => imagePaths;
        set => SetProperty(ref imagePaths, value);
    }

    public Task LoadImageAsync()
    {
        IEnumerable<string> itemPhotoPaths = paths.GetItemPhotoPaths(Item);
        foreach (var imagePath in itemPhotoPaths)
        {
            ImagePaths.Add(imagePath);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        return nav.GoToAsync("ItemDetails", new Dictionary<string, object>
        {
            ["ItemId"] = Item.ItemId.ToString()
        });
    }
}
