using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using Microsoft.Extensions.Logging;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Items.AddItem;

public partial class AddItemViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ImageService imageService;
    private readonly ICreateItemCommandHandler createItem;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly ILogger<AddItemViewModel> logger;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private ImageService.TemporaryPhotoCapture? pendingPhoto;

    [ObservableProperty]
    private string containerId = string.Empty;

    public bool IsAddingToContainer => Guid.TryParse(ContainerId, out var cid) && cid != Guid.Empty;
    public bool ShowQuantityManagement => applicationSettings.IsAdvancedMode;
    public bool ShowQuantityField => IsAddingToContainer && ShowQuantityManagement;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string quantity = "1"; // reserved for future relation quantity use

    [ObservableProperty]
    private string? validationMessage;

    [ObservableProperty]
    private string? photoThumbnailPath;

    [ObservableProperty]
    private bool isPhotoProcessing;

    public AddItemViewModel(
        ImageService imageService,
        ICreateItemCommandHandler createItem,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        ILogger<AddItemViewModel> logger,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.createItem = createItem ?? throw new ArgumentNullException(nameof(createItem));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.popupDefinitions = popupDefinitions ?? throw new ArgumentNullException(nameof(popupDefinitions));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue(NavigationParams.ContainerId, out var value) && value is string id)
        {
            ContainerId = id;
        }
    }

    partial void OnContainerIdChanged(string value)
    {
        OnPropertyChanged(nameof(IsAddingToContainer));
        OnPropertyChanged(nameof(ShowQuantityField));
    }

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

    private bool CanAdd() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand]
    private async Task ChoosePhotoAsync()
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

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task SaveAsync()
    {
        var trimmed = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ValidationMessage = "Name is required.";
            return;
        }

        var isAddingToContainer = IsAddingToContainer;
        var parsedQuantity = 1;
        if (isAddingToContainer && ShowQuantityManagement &&
            (!int.TryParse(Quantity?.Trim(), out parsedQuantity) || parsedQuantity <= 0))
        {
            ValidationMessage = "Quantity must be a positive number.";
            return;
        }

        await RunCommandAsync(async () =>
        {
            try
            {
                Guid? cid = isAddingToContainer && Guid.TryParse(ContainerId, out var parsedContainerId) && parsedContainerId != Guid.Empty
                    ? parsedContainerId
                    : null;

                await createItem.CreateAsync(
                    trimmed,
                    Description?.Trim() ?? string.Empty,
                    cid,
                    parsedQuantity,
                    pendingPhoto?.Bytes);

                if (pendingPhoto is not null)
                {
                    await imageService.DeleteTemporaryPhotoAsync(pendingPhoto.FileName);
                }

                pendingPhoto = null;
                PhotoThumbnailPath = null;
                ValidationMessage = null;
                await nav.GoBackAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save item.");
                ValidationMessage = $"Failed to save item: {ex.Message}";
            }
        });
    }
}
