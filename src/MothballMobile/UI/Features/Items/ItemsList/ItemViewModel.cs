using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading.Tasks;
using CoreApp.Contracts;

namespace MothballMobile.UI.Features.Items.ItemsList;

public partial class ItemViewModel : ItemWithImagesViewModelBase
{
    private readonly Infrastructure.INavigationService nav;

    public ItemViewModel(
        ItemInventorySummary inventory,
        IImagePathResolver paths,
        Infrastructure.INavigationService nav)
        : base(inventory, paths)
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
