using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading.Tasks;

namespace MothballMobile.UI.ViewModels;

public partial class ItemViewModel : ObservableObject
{
    public Item Item { get; }
    private readonly IImagePathResolver paths;
    private readonly Infrastructure.INavigationService nav;
    private string imagePath;

    public ItemViewModel(Item item, IImagePathResolver paths, Infrastructure.INavigationService nav)
    {
        Item = item;
        this.paths = paths;
        this.nav = nav;
        this.imagePath = "dotnet_bot.png";
    }

    public string Name => Item.Name;
    public string Description => Item.Description;

    public string ImagePath
    {
        get => imagePath;
        set => SetProperty(ref imagePath, value);
    }

    public Task LoadImageAsync()
    {
        ImagePath = paths.GetPrimaryItemPhotoPath(Item);
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
