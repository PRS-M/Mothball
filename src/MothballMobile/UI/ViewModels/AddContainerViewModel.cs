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
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly INavigationService navigationService;
    private readonly IRetryService retryService;
    private ImageService.TemporaryPhotoCapture? pendingPhoto;

    public AddContainerViewModel(
        ImageService imageService,
        IInventoryCommandRepository inventoryCommands,
        INavigationService navigationService,
        IRetryService retryService)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveContainerCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string? validationMessage;

    [ObservableProperty]
    private string? photoThumbnailPath;

    private bool CanAddContainer() => !string.IsNullOrWhiteSpace(Name);

    public bool HasTemporaryPhoto => !string.IsNullOrWhiteSpace(PhotoThumbnailPath);

    public string PhotoSelectionStatus =>
        HasTemporaryPhoto
            ? "Photo selected. It will be saved when you tap Save."
            : "No photo selected.";

    partial void OnPhotoThumbnailPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasTemporaryPhoto));
        OnPropertyChanged(nameof(PhotoSelectionStatus));
    }

    [RelayCommand]
    public async Task ChoosePhoto()
    {
        await RunCommandAsync(async () =>
        {
            ImageService.TemporaryPhotoCapture? selectedPhoto = null;

            bool photoSelected = await retryService.RetryAsync(
                async () =>
                {
                    selectedPhoto = await imageService.CaptureTemporaryPhotoAsync();
                    return selectedPhoto is not null;
                },
                canceledTitle: "Photo capture canceled",
                canceledMessage: "Please try again or continue without a photo.",
                retryButton: "Retry",
                continueButton: "Continue");

            if (!photoSelected || selectedPhoto is null)
            {
                return;
            }

            if (pendingPhoto is not null)
            {
                await imageService.DeleteTemporaryPhotoAsync(pendingPhoto.FileName);
            }

            pendingPhoto = selectedPhoto;
            PhotoThumbnailPath = selectedPhoto.FullPath;
            ValidationMessage = null;
        });
    }

    [RelayCommand(CanExecute = nameof(CanAddContainer))]
    public async Task SaveContainer()
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

            await inventoryCommands.InsertContainerAsync(container);

            if (pendingPhoto is not null)
            {
                await imageService.SaveContainerPhotoAsync(container, pendingPhoto.Bytes);
                await imageService.DeleteTemporaryPhotoAsync(pendingPhoto.FileName);
            }

            pendingPhoto = null;
            PhotoThumbnailPath = null;
            ValidationMessage = null;
            await navigationService.GoBackAsync();
        });
    }
}
