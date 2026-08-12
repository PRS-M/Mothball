using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;
using System.Threading.Tasks;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ItemWithPhotosViewModel : ItemWithImagesViewModelBase
{
    private readonly INavigationService navigation;

    public ItemWithPhotosViewModel(Item item, int quantity, IImagePathResolver paths, INavigationService navigation)
        : base(item, paths)
    {
        Quantity = quantity;
        this.navigation = navigation;
    }

    public int Quantity { get; }

    public Task LoadImagesAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        return navigation.GoToAsync(
            NavigationRoutes.ItemDetails,
            new Dictionary<string, object>
            {
                [NavigationParams.ItemId] = Item.ItemId.ToString()
            });
    }
}
