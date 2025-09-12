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
    private readonly ImageService imageService;
    private readonly IInventoryDomainRepository inventoryDomainRepository;
    private readonly INavigationService navigationService;

    public AddContainerViewModel(
        ImageService imageService,
        IInventoryDomainRepository inventoryDomainRepository,
        INavigationService navigationService)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.inventoryDomainRepository = inventoryDomainRepository ?? throw new ArgumentNullException(nameof(inventoryDomainRepository));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        Container = null!;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddContainerCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string? validationMessage;

    Container Container { get; set; }

    private bool CanAddContainer() => !string.IsNullOrWhiteSpace(this.Name);

    [RelayCommand(CanExecute = nameof(CanAddContainer))]
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

            await imageService.CaptureContainerPhotoAsync(Container);
            await inventoryDomainRepository.InsertContainerAsync(Container);

            ValidationMessage = null;
            await navigationService.GoBackAsync();
        });
    }
}
