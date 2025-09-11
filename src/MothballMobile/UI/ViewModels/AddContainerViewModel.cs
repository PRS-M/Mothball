using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Services;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public partial class AddContainerViewModel : BaseViewModel
{
    private readonly ICameraHandler cameraHandler;
    private readonly IInventoryDomainRepository inventoryDomainRepository;
    private readonly INavigationService navigationService;

    public AddContainerViewModel(ICameraHandler cameraHandler, IInventoryDomainRepository inventoryDomainRepository, INavigationService navigationService)
    {
        this.cameraHandler = cameraHandler ?? throw new ArgumentNullException(nameof(cameraHandler));
        this.inventoryDomainRepository = inventoryDomainRepository ?? throw new ArgumentNullException(nameof(inventoryDomainRepository));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        Name = string.Empty;
        Notes = string.Empty;
        Container = null!;
    }

    public string Name { get; set; }
    public string Notes { get; set; }
    Container Container { get; set; }

    [ObservableProperty]
    private string? validationMessage;

    [RelayCommand]
    public async Task AddContainer()
    {
        if (string.IsNullOrWhiteSpace(Name?.Trim()))
        {
            ValidationMessage = "Name is required.";
            return;
        }

        await RunCommandAsync(async () =>
        {
            Container = new Container(
                containerId: Guid.NewGuid(),
                name: Name.Trim(),
                notes: Notes?.Trim() ?? string.Empty
            );

            await cameraHandler.CaptureContainerPhotoAsync(Container);
            await inventoryDomainRepository.InsertContainerAsync(Container);
            if (Container.Photos is { Count: > 0 })
            {
                await inventoryDomainRepository.InsertImageItemAsync(Container.Photos[0], Container.ContainerId);
            }

            ValidationMessage = null;
            await navigationService.GoBackAsync();
        });
    }
}
