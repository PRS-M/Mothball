using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading.Tasks;

namespace MothballMobile.UI.Features.Items.ItemsList;

public partial class ItemViewModel : ItemWithImagesViewModelBase
{
    private readonly Infrastructure.INavigationService nav;

    public ItemViewModel(Item item, IImagePathResolver paths, Infrastructure.INavigationService nav)
        : base(item, paths)
    {
        this.nav = nav;
    }

    public Task LoadImageAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        return nav.GoToAsync(Infrastructure.NavigationRoutes.ItemDetails, new Dictionary<string, object>
        {
            [Infrastructure.NavigationParams.ItemId] = Item.ItemId.ToString()
        });
    }
}
