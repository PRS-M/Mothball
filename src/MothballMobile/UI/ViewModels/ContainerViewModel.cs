using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MothballMobile.UI.ViewModels;

public partial class ContainerViewModel : ObservableObject
{
    public Container Container { get; }
    public Dictionary<string, List<string>> ItemIdsByContainerId { get; } = new();
    private readonly IImagePathResolver _paths;
    private ObservableCollection<string> _imagePaths;

    public ObservableCollection<string> ImagePaths
    {
        get => _imagePaths;
        set => SetProperty(ref _imagePaths, value);
    }

    public string Name => Container.Name;
    public string Notes => Container.Notes;
    public string ItemCount => $"Items stored: {Container.ItemCount}";

    public ContainerViewModel(Container container, IImagePathResolver paths)
    {
        Container = container;
        _paths = paths;
        _imagePaths = new ObservableCollection<string>();
    }

    public Task LoadImageAsync()
    {
        foreach (var path in _paths.GetContainerPhotoPaths(Container))
            ImagePaths.Add(path);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task NavigateAsync()
    {
        var id = Container.ContainerId.ToString();
        var nav = Application.Current?.Handler?.MauiContext?.Services?.GetService<Infrastructure.INavigationService>();
        return nav?.GoToAsync("ContainerDetails", new Dictionary<string, object> { ["ContainerId"] = id })
               ?? Task.CompletedTask;
    }
}
