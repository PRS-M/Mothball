using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;

namespace MothballMobile.UI.ViewModels;

public partial class SelectableContainerViewModel : ContainerWithImagesViewModelBase
{
    private readonly Func<Guid, Task> select;

    public SelectableContainerViewModel(Container container, IImagePathResolver paths, Func<Guid, Task> select)
        : base(container, paths)
    {
        this.select = select;
    }

    public Task LoadImagesAsync()
    {
        return LoadContainerImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task SelectAsync() => select(Container.ContainerId);
}
