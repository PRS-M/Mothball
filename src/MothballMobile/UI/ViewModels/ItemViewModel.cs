using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading.Tasks;

namespace MothballMobile.UI.ViewModels;

public partial class ItemViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IImagePathResolver _paths;
    private readonly Infrastructure.INavigationService _nav;
    private string _imagePath;

    public ItemViewModel(Item item, IImagePathResolver paths, Infrastructure.INavigationService nav)
    {
        Item = item;
        _paths = paths;
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
        ImagePath = _paths.GetPrimaryItemPhotoPath(Item);
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
