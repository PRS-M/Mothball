using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Features.Photos;
using Microsoft.Extensions.Logging;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Items.AddItem;

public partial class AddItemViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ICreateItemCommandHandler createItem;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly ILogger<AddItemViewModel> logger;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly PendingPhoto pendingPhoto;

    [ObservableProperty]
    private string containerId = string.Empty;

    public bool IsAddingToContainer => Guid.TryParse(ContainerId, out var cid) && cid != Guid.Empty;
    public bool ShowQuantityManagement => applicationSettings.IsAdvancedMode;
    public bool ShowQuantityField => ShowQuantityManagement;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string quantity = "1";

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
        this.createItem = createItem ?? throw new ArgumentNullException(nameof(createItem));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.popupDefinitions = popupDefinitions ?? throw new ArgumentNullException(nameof(popupDefinitions));
        pendingPhoto = new PendingPhoto(imageService ?? throw new ArgumentNullException(nameof(imageService)));
    }

    /// <inheritdoc />
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
            ? Localization.Current.Get("Processing photo...")
            : HasTemporaryPhoto
            ? Localization.Current.Get("Photo selected. It will be saved when you tap Save.")
            : Localization.Current.Get("No photo selected.");

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

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task SaveAsync()
    {
        var trimmed = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ValidationMessage = Localization.Current.Get("Name is required.");
            return;
        }

        var isAddingToContainer = IsAddingToContainer;
        var parsedQuantity = 1;
        if (ShowQuantityManagement &&
            (!int.TryParse(Quantity?.Trim(), out parsedQuantity) || parsedQuantity <= 0))
        {
            ValidationMessage = Localization.Current.Get("Quantity must be a positive number.");
            return;
        }

        await RunCommandAsync(async () =>
        {
            Guid? cid = isAddingToContainer && Guid.TryParse(ContainerId, out var parsedContainerId) && parsedContainerId != Guid.Empty
                ? parsedContainerId
                : null;

            try
            {
                await createItem.CreateAsync(
                    trimmed,
                    Description?.Trim() ?? string.Empty,
                    cid,
                    parsedQuantity,
                    pendingPhoto.Bytes);
            }
            catch (Exception ex)
            {
                // Log locally, then rethrow so RunCommandAsync surfaces it through the shared error banner.
                logger.LogError(ex, "Failed to save item.");
                throw;
            }

            await pendingPhoto.DiscardAsync();
            PhotoThumbnailPath = null;
            ValidationMessage = null;
            await nav.GoBackAsync();
        });
    }
}
