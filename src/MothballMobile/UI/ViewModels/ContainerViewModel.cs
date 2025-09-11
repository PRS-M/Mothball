using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;

namespace MothballMobile.UI.ViewModels;

public partial class ContainerViewModel : ObservableObject
{
    public Container Container { get; }
    private readonly IImagePathResolver paths;
    private readonly Infrastructure.INavigationService nav;
    private ObservableCollection<string> imagePaths;

    public ObservableCollection<string> ImagePaths
    {
        get => imagePaths;
        set => SetProperty(ref imagePaths, value);
    }

    public string Name => Container.Name;
    public string Notes => Container.Notes;
    public string ItemCount => $"Items stored: {Container.ItemCount}";

    public ContainerViewModel(Container container, IImagePathResolver paths, Infrastructure.INavigationService nav)
    {
        Container = container;
        this.paths = paths;
        this.nav = nav;
        this.imagePaths = new ObservableCollection<string>();
    }

    public Task LoadImageAsync()
    {
        foreach (var path in paths.GetContainerPhotoPaths(Container))
            ImagePaths.Add(path);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task NavigateAsync()
    {
        var id = Container.ContainerId.ToString();
        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.ContainerDetails,
            new Dictionary<string, object> { [Infrastructure.NavigationParams.ContainerId] = id }
        );
    }
}
