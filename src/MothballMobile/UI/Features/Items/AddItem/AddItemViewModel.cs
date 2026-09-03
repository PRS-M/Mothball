using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Features.Photos;
using CoreApp.Application.Utilities;
using CoreApp.Domain.Entities.Shared;
using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure.Scanning;
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
    private readonly IBarcodeScanSession barcodeScanner;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IItemReceiptService itemReceipts;

    private static readonly ReadOnlyCollection<BarcodeSymbology> extendedBarcodeSymbologies = EnumValues.CreateReadOnly<BarcodeSymbology>();
    private static readonly ReadOnlyCollection<BarcodeSymbology> qrCodeOnlySymbologies = new([BarcodeSymbology.QrCode]);

    public IReadOnlyList<BarcodeSymbology> AvailableBarcodeSymbologies => applicationSettings.IsBarcodeExtendedMode
        ? extendedBarcodeSymbologies
        : qrCodeOnlySymbologies;

    [ObservableProperty]
    private string containerId = string.Empty;

    public bool IsAddingToContainer => Guid.TryParse(ContainerId, out var cid) && cid != Guid.Empty;
    public bool ShowQuantityManagement => applicationSettings.IsAdvancedMode;
    public bool ShowQuantityField => ShowQuantityManagement || IsReceivingExistingItem;
    public bool IsReceivingExistingItem { get; private set; }
    public bool IsItemMetadataEditable => !IsReceivingExistingItem;

    [ObservableProperty]
    private string destinationContainerName = string.Empty;

    public bool HasDestinationContainer => !string.IsNullOrWhiteSpace(DestinationContainerName);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string quantity = "1";

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

    public AddItemViewModel(
        ImageService imageService,
        ICreateItemCommandHandler createItem,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        ILogger<AddItemViewModel> logger,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IBarcodeScanSession barcodeScanner,
        IInventoryQueryRepository inventoryQueries,
        IItemReceiptService itemReceipts)
    {
        this.createItem = createItem ?? throw new ArgumentNullException(nameof(createItem));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.popupDefinitions = popupDefinitions ?? throw new ArgumentNullException(nameof(popupDefinitions));
        this.barcodeScanner = barcodeScanner ?? throw new ArgumentNullException(nameof(barcodeScanner));
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.itemReceipts = itemReceipts ?? throw new ArgumentNullException(nameof(itemReceipts));
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

    partial void OnDestinationContainerNameChanged(string value)
        => OnPropertyChanged(nameof(HasDestinationContainer));

    public bool HasTemporaryPhoto => !string.IsNullOrWhiteSpace(PhotoThumbnailPath);

    public bool ShowPhotoThumbnail => HasTemporaryPhoto && !IsPhotoProcessing;

    public bool ShowPhotoProcessingIndicator => IsPhotoProcessing;

    public string PhotoSelectionStatus =>
        IsPhotoProcessing
            ? LocalizationManager.Current.Get("Processing photo...")
            : HasTemporaryPhoto
            ? LocalizationManager.Current.Get("Photo selected. It will be saved when you tap Save.")
            : LocalizationManager.Current.Get("No photo selected.");

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

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        await RunCommandAsync(async () =>
        {
            var barcode = await barcodeScanner.ScanAsync();
            if (barcode is null)
            {
                return;
            }

            BarcodeValue = barcode.Value;
            BarcodeSymbology = barcode.Symbology;

            await ResolveBarcodeCoreAsync();
        }, rethrowOnError: false);
    }

    [RelayCommand]
    private Task ResolveBarcodeAsync()
        => RunCommandAsync(ResolveBarcodeCoreAsync, rethrowOnError: false);

    private async Task ResolveBarcodeCoreAsync()
    {
        var normalizedBarcodeValue = BarcodeValue?.Trim() ?? string.Empty;
        ValidationMessage = null;
        if (!string.Equals(BarcodeValue, normalizedBarcodeValue, StringComparison.Ordinal))
        {
            BarcodeValue = normalizedBarcodeValue;
        }

        if (string.IsNullOrWhiteSpace(normalizedBarcodeValue))
        {
            ResetReceiptMode();
            return;
        }

        var existingOwner = await inventoryQueries.FindBarcodeAsync(normalizedBarcodeValue);
        if (!string.Equals(BarcodeValue, normalizedBarcodeValue, StringComparison.Ordinal))
        {
            return;
        }

        if (existingOwner?.OwnerKind != BarcodeOwnerKind.Item)
        {
            ResetReceiptMode();
            if (existingOwner?.OwnerKind == BarcodeOwnerKind.Container)
            {
                ValidationMessage = LocalizationManager.Current.Get("This barcode is already assigned to a container.");
            }
            return;
        }

        Name = existingOwner.OwnerName;
        IsReceivingExistingItem = true;
        OnPropertyChanged(nameof(ShowQuantityField));
        OnPropertyChanged(nameof(IsItemMetadataEditable));
    }

    private void ResetReceiptMode()
    {
        if (!IsReceivingExistingItem)
        {
            return;
        }

        IsReceivingExistingItem = false;
        Name = string.Empty;
        Description = string.Empty;
        Quantity = "1";
        DestinationContainerName = string.Empty;
        OnPropertyChanged(nameof(ShowQuantityField));
        OnPropertyChanged(nameof(IsItemMetadataEditable));
    }

    [RelayCommand]
    private async Task ScanDestinationContainerAsync()
    {
        if (!IsReceivingExistingItem)
        {
            return;
        }

        await RunCommandAsync(async () =>
        {
            var barcode = await barcodeScanner.ScanAsync();
            if (barcode is null)
            {
                return;
            }

            var owner = await inventoryQueries.FindBarcodeAsync(barcode.Value);
            if (owner?.OwnerKind != BarcodeOwnerKind.Container)
            {
                return;
            }

            ContainerId = owner.OwnerId.ToString();
            DestinationContainerName = owner.OwnerName;
        }, rethrowOnError: false);
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task SaveAsync()
    {
        var trimmed = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ValidationMessage = LocalizationManager.Current.Get("Name is required.");
            return;
        }

        var isAddingToContainer = IsAddingToContainer;
        var parsedQuantity = 1;
        if ((ShowQuantityManagement || IsReceivingExistingItem) &&
            (!int.TryParse(Quantity?.Trim(), out parsedQuantity) || parsedQuantity <= 0))
        {
            ValidationMessage = LocalizationManager.Current.Get("Quantity must be a positive number.");
            return;
        }

        await RunCommandAsync(async () =>
        {
            var destinationContainerId = GetDestinationContainerId(isAddingToContainer);

            if (IsReceivingExistingItem)
            {
                await ReceiveExistingItemAsync(parsedQuantity, destinationContainerId);
                await nav.GoBackAsync();
                return;
            }

            await CreateItemAsync(trimmed, parsedQuantity, destinationContainerId);

            await pendingPhoto.DiscardAsync();
            PhotoThumbnailPath = null;
            ValidationMessage = null;
            await nav.GoBackAsync();
        }, errorMessageFactory: BarcodeOperationErrorMessage, rethrowOnError: false);
    }

    private static string BarcodeOperationErrorMessage(Exception exception)
        => exception is BarcodeAlreadyAssignedException
            ? LocalizationManager.Current.Get("This barcode is already in use.")
            : LocalizationManager.Current.Get("Something went wrong. Please try again.");

    private Guid? GetDestinationContainerId(bool isAddingToContainer)
        => isAddingToContainer && Guid.TryParse(ContainerId, out var parsedContainerId) && parsedContainerId != Guid.Empty
            ? parsedContainerId
            : null;

    private async Task ReceiveExistingItemAsync(int quantity, Guid? containerId)
    {
        var existingItem = await inventoryQueries.FindBarcodeAsync(BarcodeValue);
        if (existingItem?.OwnerKind != BarcodeOwnerKind.Item)
        {
            throw new InvalidOperationException("The scanned item barcode is no longer available.");
        }

        await itemReceipts.ReceiveAsync(existingItem.OwnerId, quantity, containerId);
    }

    private async Task CreateItemAsync(string name, int quantity, Guid? containerId)
    {
        var normalizedBarcodeValue = BarcodeValue?.Trim();
        var barcode = string.IsNullOrWhiteSpace(normalizedBarcodeValue)
            ? null
            : new Barcode(normalizedBarcodeValue, BarcodeSymbology);

        try
        {
            await createItem.CreateAsync(
                name,
                Description?.Trim() ?? string.Empty,
                containerId,
                quantity,
                pendingPhoto.Bytes,
                barcode);
        }
        catch (Exception ex)
        {
            // Log locally, then rethrow so RunCommandAsync surfaces it through the shared error banner.
            logger.LogError(ex, "Failed to save item.");
            throw;
        }
    }
}
