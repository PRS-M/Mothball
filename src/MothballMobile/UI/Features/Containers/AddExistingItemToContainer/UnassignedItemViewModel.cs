using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;

namespace MothballMobile.UI.Features.Containers.AddExistingItemToContainer;

public partial class UnassignedItemViewModel : ItemWithImagesViewModelBase
{
    private readonly Func<Guid, Task> assign;

    public UnassignedItemViewModel(Item item, IImagePathResolver paths, Func<Guid, Task> assign)
        : base(item, paths)
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
