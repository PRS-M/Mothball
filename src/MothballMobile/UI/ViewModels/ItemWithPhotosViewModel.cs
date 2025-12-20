using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using System.Threading.Tasks;

namespace MothballMobile.UI.ViewModels;

public class ItemWithPhotosViewModel : ItemWithImagesViewModelBase
{
    public ItemWithPhotosViewModel(Item item, IImagePathResolver paths)
        : base(item, paths)
    {
    }

    public Task LoadImagesAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }
}
