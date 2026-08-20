using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Domain.Entities.ContainerAggregate;

namespace MothballMobile.UI.Shared;

public abstract class ContainerWithImagesViewModelBase : ObservableObject
{
    private readonly IImagePathResolver paths;

    protected ContainerWithImagesViewModelBase(Container container, IImagePathResolver paths)
    {
        Container = container;
        this.paths = paths;
    }

    public Container Container { get; }

    public string Name => Container.Name;
    public string Notes => Container.Notes;
    public string ItemCount => $"Items stored: {Container.ItemCount}";

    public ObservableCollection<string> ImagePaths { get; } = new();

    /// <summary>
    /// Loads this container's photo paths into <see cref="ImagePaths"/>.
    /// </summary>
    /// <param name="clearFirst">Whether to clear existing paths before loading.</param>
    protected Task LoadContainerImagesAsync(bool clearFirst = true)
    {
        if (clearFirst)
        {
            ImagePaths.Clear();
        }

        foreach (var path in paths.GetContainerPhotoPaths(Container))
        {
            ImagePaths.Add(path);
        }

        return Task.CompletedTask;
    }
}
