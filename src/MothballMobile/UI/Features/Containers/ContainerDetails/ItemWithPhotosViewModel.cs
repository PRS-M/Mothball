using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;
using System.Threading.Tasks;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public partial class ItemWithPhotosViewModel : ItemWithImagesViewModelBase
{
    private readonly INavigationService navigation;
    private readonly string? sourceContainerId;

    public ItemWithPhotosViewModel(
        Item item,
        int quantity,
        IImagePathResolver paths,
        INavigationService navigation,
        string? sourceContainerId)
        : base(item, paths)
    {
        Quantity = quantity;
        this.navigation = navigation;
        this.sourceContainerId = sourceContainerId;
    }

    public int Quantity { get; }

    public Task LoadImagesAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = Item.ItemId.ToString()
        };

        if (!string.IsNullOrWhiteSpace(sourceContainerId))
        {
            parameters[NavigationParams.ContainerId] = sourceContainerId;
        }

        return navigation.GoToAsync(
            NavigationRoutes.ItemDetails,
            parameters);
    }
}
