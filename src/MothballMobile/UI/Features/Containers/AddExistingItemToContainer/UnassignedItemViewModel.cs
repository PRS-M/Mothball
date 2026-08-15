using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Contracts;

namespace MothballMobile.UI.Features.Containers.AddExistingItemToContainer;

public partial class UnassignedItemViewModel : ItemWithImagesViewModelBase
{
    private readonly Func<Guid, Task> assign;

    public UnassignedItemViewModel(
        ItemInventorySummary inventory,
        IImagePathResolver paths,
        Func<Guid, Task> assign)
        : base(inventory, paths)
    {
        this.assign = assign;
    }

    public Task LoadImagesAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task AssignToContainerAsync() => assign(Item.ItemId);
}
