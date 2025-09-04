using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Services;

namespace MothballMobile.UI.ViewModels;

public partial class AddContainerViewModel : ObservableObject
{
    private readonly ICameraHandler cameraHandler;

    public AddContainerViewModel(ICameraHandler cameraHandler)
    {
        this.cameraHandler = cameraHandler ?? throw new ArgumentNullException(nameof(cameraHandler));
        Name = string.Empty;
        Description = string.Empty;
        LocationDescription = string.Empty;
        UniqueId = string.Empty;
        Photo = null!;
        Container = null!;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public string LocationDescription { get; set; }
    public string UniqueId { get; set; }
    public ImageItem Photo { get; set; }
    Container Container { get; set; }

    [RelayCommand]
    public async Task AddContainer()
    {
        Container = new Container(
            uniqueId: Guid.NewGuid().ToString(),
            name: Name,
            description: Description
        );
    }
}
