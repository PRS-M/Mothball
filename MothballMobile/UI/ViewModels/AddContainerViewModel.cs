using System;
using System.Threading.Tasks;
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
    private readonly IInventoryDomainRepository inventoryDomainRepository;

    public AddContainerViewModel(ICameraHandler cameraHandler, IInventoryDomainRepository inventoryDomainRepository)
    {
        this.cameraHandler = cameraHandler ?? throw new ArgumentNullException(nameof(cameraHandler));
        this.inventoryDomainRepository = inventoryDomainRepository ?? throw new ArgumentNullException(nameof(inventoryDomainRepository));
        Name = string.Empty;
        Notes = string.Empty;
        Container = null!;
    }

    public string Name { get; set; }
    public string Notes { get; set; }
    Container Container { get; set; }

    [RelayCommand]
    public async Task AddContainer()
    {
        Container = new Container(
            containerId: Guid.NewGuid(),
            name: Name,
            notes: Notes
        );

        await cameraHandler.CaptureContainerPhotoAsync(Container);
        await inventoryDomainRepository.InsertContainerAsync(Container);
        if (Container.Photos is { Count: > 0 })
        {
            await inventoryDomainRepository.InsertImageItemAsync(Container.Photos[0], Container.ContainerId);
        }
        await Shell.Current.GoToAsync("..");
    }
}
