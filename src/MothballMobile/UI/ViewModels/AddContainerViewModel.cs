using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public partial class AddContainerViewModel : BaseViewModel
{
    private readonly ImageService imageService;
    private readonly IInventoryDomainRepository inventoryDomainRepository;
    private readonly INavigationService navigationService;
    private readonly IRetryService retryService;

    public AddContainerViewModel(
        ImageService imageService,
        IInventoryDomainRepository inventoryDomainRepository,
        INavigationService navigationService,
        IRetryService retryService)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.inventoryDomainRepository = inventoryDomainRepository ?? throw new ArgumentNullException(nameof(inventoryDomainRepository));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddContainerCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string? validationMessage;

    private bool CanAddContainer() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanAddContainer))]
    public async Task AddContainer()
    {
        var trimmedName = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ValidationMessage = "Name is required.";
            return;
        }

        await RunCommandAsync(async () =>
        {
            var container = new Container(
                containerId: Guid.NewGuid(),
                name: trimmedName,
                notes: string.IsNullOrWhiteSpace(Notes) ? string.Empty : Notes.Trim()
            );

            await CapturePhotoWithOptionalRetryAsync(container);
            await inventoryDomainRepository.InsertContainerAsync(container);

            ValidationMessage = null;
            await navigationService.GoBackAsync();
        });
    }

    private async Task CapturePhotoWithOptionalRetryAsync(Container container)
    {
        await retryService.RetryAsync(
            async () =>
            {
                var bytesLength = await imageService.CaptureContainerPhotoAsync(container);
                return bytesLength > 0;
            },
            canceledTitle: "Photo capture canceled",
            canceledMessage: "Please try again or continue without a photo.",
            retryButton: "Retry",
            continueButton: "Continue",
            continueAlertTitle: "No photo",
            continueAlertMessage: "Continuing without a photo.");
    }
}
