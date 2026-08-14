using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Items.AddItem;

public partial class AddItemViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ImageService imageService;
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly Infrastructure.INavigationService nav;
    private readonly ILogger<AddItemViewModel> logger;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private ImageService.TemporaryPhotoCapture? pendingPhoto;

    [ObservableProperty]
    private string containerId = string.Empty;

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
        IInventoryCommandRepository inventoryCommands,
        Infrastructure.INavigationService nav,
        ILogger<AddItemViewModel> logger,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
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

        if (!int.TryParse(Quantity?.Trim(), out var parsedQuantity) || parsedQuantity <= 0)
        {
            ValidationMessage = "Quantity must be a positive number.";
            return;
        }

        await RunCommandAsync(async () =>
        {
            try
            {
                var item = new Item(trimmed, Description?.Trim() ?? string.Empty);

                await inventoryCommands.InsertItemAsync(item);

                if (pendingPhoto is not null)
                {
                    await imageService.SaveItemPhotoAsync(item, pendingPhoto.Bytes);
                    await imageService.DeleteTemporaryPhotoAsync(pendingPhoto.FileName);
                }

                if (Guid.TryParse(ContainerId, out var cid) && cid != Guid.Empty)
                {
                    await inventoryCommands.InsertItemContainerRelation(item.ItemId, cid, parsedQuantity);
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
