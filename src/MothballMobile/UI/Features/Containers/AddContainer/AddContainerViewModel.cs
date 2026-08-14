using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using CoreApp.Services;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Containers.AddContainer;

public partial class AddContainerViewModel : BaseViewModel
{
    private readonly ImageService imageService;
    private readonly ICreateContainerCommandHandler createContainer;
    private readonly INavigationService navigationService;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private ImageService.TemporaryPhotoCapture? pendingPhoto;

    public AddContainerViewModel(
        ImageService imageService,
        ICreateContainerCommandHandler createContainer,
        INavigationService navigationService,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.createContainer = createContainer ?? throw new ArgumentNullException(nameof(createContainer));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.popupDefinitions = popupDefinitions ?? throw new ArgumentNullException(nameof(popupDefinitions));
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

    [ObservableProperty]
    private bool isPhotoProcessing;

    private bool CanAddContainer() => !string.IsNullOrWhiteSpace(Name);

    public bool HasTemporaryPhoto => !string.IsNullOrWhiteSpace(PhotoThumbnailPath);

    public bool ShowPhotoThumbnail => HasTemporaryPhoto && !IsPhotoProcessing;

    public bool ShowPhotoProcessingIndicator => IsPhotoProcessing;

    public string PhotoSelectionStatus =>
        IsPhotoProcessing
            ? "Processing photo..."
            : HasTemporaryPhoto
            ? "Photo selected. It will be saved when you tap Save."
            : "No photo selected.";

    partial void OnPhotoThumbnailPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasTemporaryPhoto));
        OnPropertyChanged(nameof(ShowPhotoThumbnail));
        OnPropertyChanged(nameof(PhotoSelectionStatus));
    }

    partial void OnIsPhotoProcessingChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPhotoThumbnail));
        OnPropertyChanged(nameof(ShowPhotoProcessingIndicator));
        OnPropertyChanged(nameof(PhotoSelectionStatus));
    }

    [RelayCommand]
    public async Task ChoosePhoto()
    {
        if (IsBusy)
        {
            return;
        }

        var source = await SelectPhotoSourceAsync();
        if (source is null)
        {
            return;
        }

        await RunCommandAsync(async () =>
        {
            ImageService.TemporaryPhotoCapture? selectedPhoto;
            IsPhotoProcessing = true;
            try
            {
                selectedPhoto = await imageService.CaptureTemporaryPhotoAsync(source: source.Value);
            }
            finally
            {
                IsPhotoProcessing = false;
            }

            if (selectedPhoto is null)
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

    private async Task<PhotoSource?> SelectPhotoSourceAsync()
        => await PhotoSourceSelector.SelectPhotoSourceAsync(popup, popupDefinitions);

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
            await createContainer.CreateAsync(
                trimmedName,
                string.IsNullOrWhiteSpace(Notes) ? string.Empty : Notes.Trim(),
                pendingPhoto?.Bytes);

            if (pendingPhoto is not null)
            {
                await imageService.DeleteTemporaryPhotoAsync(pendingPhoto.FileName);
            }

            pendingPhoto = null;
            PhotoThumbnailPath = null;
            ValidationMessage = null;
            await navigationService.GoBackAsync();
        });
    }
}
