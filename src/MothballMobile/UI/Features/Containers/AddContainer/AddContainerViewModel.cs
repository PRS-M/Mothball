using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Features.Photos;
using CoreApp.Application.Utilities;
using CoreApp.Domain.Entities.Shared;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Containers.AddContainer;


public partial class AddContainerViewModel : BaseViewModel
{
    private readonly ICreateContainerCommandHandler createContainer;
    private readonly INavigationService navigationService;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly PendingPhoto pendingPhoto;
    private readonly IBarcodeScanSession barcodeScanner;

    public static ReadOnlyCollection<BarcodeSymbology> AvailableBarcodeSymbologies { get; } = EnumValues.CreateReadOnly<BarcodeSymbology>();

    public AddContainerViewModel(
        ImageService imageService,
        ICreateContainerCommandHandler createContainer,
        INavigationService navigationService,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IBarcodeScanSession barcodeScanner)
    {
        this.createContainer = createContainer ?? throw new ArgumentNullException(nameof(createContainer));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.popupDefinitions = popupDefinitions ?? throw new ArgumentNullException(nameof(popupDefinitions));
        this.barcodeScanner = barcodeScanner ?? throw new ArgumentNullException(nameof(barcodeScanner));
        pendingPhoto = new PendingPhoto(imageService ?? throw new ArgumentNullException(nameof(imageService)));
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveContainerCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string barcodeValue = string.Empty;

    [ObservableProperty]
    private BarcodeSymbology barcodeSymbology = BarcodeSymbology.QrCode;

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

    /// <summary>
    /// Prompts for a photo source and stages the selected photo for the new container.
    /// </summary>
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
            IsPhotoProcessing = true;
            try
            {
                if (!await pendingPhoto.CaptureAsync(source.Value))
                {
                    return;
                }
            }
            finally
            {
                IsPhotoProcessing = false;
            }

            PhotoThumbnailPath = pendingPhoto.FullPath;
            ValidationMessage = null;
        });
    }

    private async Task<PhotoSource?> SelectPhotoSourceAsync()
        => await PhotoSourceSelector.SelectPhotoSourceAsync(popup, popupDefinitions);

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        var barcode = await barcodeScanner.ScanAsync();
        if (barcode is null)
        {
            return;
        }

        BarcodeValue = barcode.Value;
        BarcodeSymbology = barcode.Symbology;
    }

    /// <summary>
    /// Creates the container and persists its staged photo, if one was selected.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddContainer))]
    public async Task SaveContainer()
    {
        var trimmedName = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ValidationMessage = LocalizationManager.Current.Get("Name is required.");
            return;
        }

        await RunCommandAsync(async () =>
        {
            var normalizedBarcodeValue = BarcodeValue?.Trim();
            var barcode = string.IsNullOrWhiteSpace(normalizedBarcodeValue)
                ? null
                : new Barcode(normalizedBarcodeValue, BarcodeSymbology);
            await createContainer.CreateAsync(
                trimmedName,
                string.IsNullOrWhiteSpace(Notes) ? string.Empty : Notes.Trim(),
                pendingPhoto.Bytes,
                barcode);

            await pendingPhoto.DiscardAsync();
            PhotoThumbnailPath = null;
            ValidationMessage = null;
            await navigationService.GoBackAsync();
        });
    }
}
