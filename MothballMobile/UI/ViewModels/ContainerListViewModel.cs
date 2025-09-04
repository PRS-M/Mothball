using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Utilities;
using CoreApp.Entities;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Entities.ContainerAggregate;

namespace MothballMobile.UI.ViewModels;
public partial class ContainerListViewModel : ObservableObject
{

    private ObservableCollection<ContainerViewModel> containers = new();
    public ObservableCollection<ContainerViewModel> Containers
    {
        get => containers;
        set => SetProperty(ref containers, value);
    }

    private readonly IFileHandler _fileHandler;
    private readonly int _pageSize = 10;
    private int _currentPage = 0;
    private bool _isLoading;
    private List<string> _allContainerIds;

    public ContainerListViewModel(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
        Containers = new ObservableCollection<ContainerViewModel>();
        _allContainerIds = new List<string>();
    }

    public async Task InitializeAsync()
    {
        // Load all container IDs from the data source
        // Utilize repositories or services to fetch containers
    }

    [RelayCommand]
    public async Task LoadNextPageAsync()
    {
        // Implement pagination logic to load the next set of containers
    }

    // Optional: placeholder navigate command
    [RelayCommand]
    private Task NavigateAsync(ContainerViewModel? vm)
    {
        return Task.CompletedTask;
    }
}

public class ContainerViewModel : ObservableObject
{
    public Container Container { get; }
    public Dictionary<string, List<string>> ItemIdsByContainerId { get; } = new();
    private readonly IFileHandler _fileHandler;
    private ImageSource _imageSource;
    public ImageSource ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public string Name => Container.Name;
    public string Description => Container.Notes;
    public int ItemCount => Container.ItemCount;

    public ContainerViewModel(Container container, IFileHandler fileHandler)
    {
        Container = container;
        _fileHandler = fileHandler;
        _imageSource = "dotnet_bot.png";
    }

    public async Task LoadImageAsync()
    {
        if (Container.Photos != null && Container.Photos.Any(p => !string.IsNullOrEmpty(p.FileName)))
        {
            try
            {
                // TODO: Multiple photos handling.
                using var ms = await _fileHandler.GetImageMemoryStream(Container.Photos[0].FileName, Constants.PathToContainerPhotos);
                ImageSource = ImageSource.FromStream(() => ms);
            }
            catch
            {
                ImageSource = "dotnet_bot.png";
            }
        }
        else
        {
            ImageSource = "dotnet_bot.png";
        }
    }
}
