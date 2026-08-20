using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;

namespace MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

public partial class SelectableContainerViewModel : ContainerWithImagesViewModelBase
{
    private readonly Func<Guid, Task> select;

    public SelectableContainerViewModel(
        Container container,
        IImagePathResolver paths,
        Func<Guid, Task> select,
        bool showQuantityManagement)
        : base(container, paths)
    {
        this.select = select;
        ShowQuantityManagement = showQuantityManagement;
    }

    public bool ShowQuantityManagement { get; }

    /// <summary>
    /// Loads the container's photo paths for display.
    /// </summary>
    public Task LoadImagesAsync()
    {
        return LoadContainerImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task SelectAsync() => select(Container.ContainerId);
}
