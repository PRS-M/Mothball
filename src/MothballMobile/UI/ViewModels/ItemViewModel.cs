using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Utilities;
using CoreApp.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace MothballMobile.UI.ViewModels;

public partial class ItemViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IFileHandler _fileHandler;
    private readonly Infrastructure.INavigationService _nav;
    private string _imagePath;

    public ItemViewModel(Item item, IFileHandler fileHandler, Infrastructure.INavigationService nav)
    {
        Item = item;
        _fileHandler = fileHandler;
        _nav = nav;
        _imagePath = "dotnet_bot.png";
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public string ImagePath
    {
        get => _imagePath;
        set => SetProperty(ref _imagePath, value);
    }

    public Task LoadImageAsync()
    {
        if (Item.Photos != null && Item.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
            var path = Path.Combine(_fileHandler.GetAppDataPath(), Constants.PathToItemPhotos, Item.Photos[0].FileName);
            ImagePath = path;
        }
        else
        {
            ImagePath = "dotnet_bot.png";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        return _nav.GoToAsync("ItemDetails", new Dictionary<string, object>
        {
            ["ItemId"] = Item.ItemId.ToString()
        });
    }
}
