using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts;
using CoreApp.Domain.ValueObjects;

namespace MothballMobile.Infrastructure.Presentation.Popups;

/// <summary>
/// Defines reusable popup definitions for application workflows.
/// </summary>
public interface IPopupDefinitionService
{
    /// <summary>
    /// Creates an alert confirming that a backup was exported and showing its path.
    /// </summary>
    /// <param name="fullPath">The value used by the operation.</param>
    AlertPopupDefinition BackupExported(string fullPath);

    /// <summary>
    /// Creates an alert describing a backup export failure.
    /// </summary>
    /// <param name="errorMessage">The value used by the operation.</param>
    AlertPopupDefinition BackupExportFailed(string errorMessage);

    /// <summary>
    /// Creates an alert describing a backup-sharing failure.
    /// </summary>
    /// <param name="errorMessage">The value used by the operation.</param>
    AlertPopupDefinition BackupShareFailed(string errorMessage);

    /// <summary>
    /// Creates an alert describing a backup signing-key sharing failure.
    /// </summary>
    /// <param name="errorMessage">The value used by the operation.</param>
    AlertPopupDefinition BackupSigningKeyShareFailed(string errorMessage);

    /// <summary>
    /// Creates an alert describing a backup signing-key import failure.
    /// </summary>
    /// <param name="errorMessage">The value used by the operation.</param>
    AlertPopupDefinition BackupSigningKeyImportFailed(string errorMessage);

    /// <summary>
    /// Creates an alert summarizing a completed backup restore.
    /// </summary>
    /// <param name="summary">The value used by the operation.</param>
    AlertPopupDefinition RestoreCompleted(string summary);

    /// <summary>
    /// Creates an alert describing a backup restore failure.
    /// </summary>
    /// <param name="errorMessage">The value used by the operation.</param>
    AlertPopupDefinition RestoreFailed(string errorMessage);

    /// <summary>
    /// Creates a confirmation prompt for deleting a backup file.
    /// </summary>
    /// <param name="fileName">The value used by the operation.</param>
    ConfirmationPopupDefinition DeleteBackup(string fileName);

    /// <summary>
    /// Creates an alert confirming that a backup file was deleted.
    /// </summary>
    /// <param name="fileName">The value used by the operation.</param>
    AlertPopupDefinition BackupDeleted(string fileName);

    /// <summary>
    /// Creates an alert describing a backup deletion failure.
    /// </summary>
    /// <param name="errorMessage">The value used by the operation.</param>
    AlertPopupDefinition DeleteBackupFailed(string errorMessage);

    /// <summary>
    /// Creates a picker for selecting a backup restore conflict policy.
    /// </summary>
    OptionPickerPopupDefinition<InventoryBackupConflictPolicy> RestorePolicyPicker();

    /// <summary>
    /// Creates an alert stating that no backup files are available.
    /// </summary>
    AlertPopupDefinition NoBackupsFound();

    /// <summary>
    /// Creates a picker for selecting a backup file.
    /// </summary>
    /// <param name="fileNames">The value used by the operation.</param>
    OptionPickerPopupDefinition<string> BackupFilePicker(IReadOnlyList<string> fileNames);

    /// <summary>
    /// Creates a picker for selecting a photo source.
    /// </summary>
    OptionPickerPopupDefinition<PhotoSource> PhotoSourcePicker();

    /// <summary>
    /// Creates a picker for selecting an item photo to delete.
    /// </summary>
    /// <param name="photos">The value used by the operation.</param>
    OptionPickerPopupDefinition<ImageItem> ItemPhotoDeletePicker(IReadOnlyList<ImageItem> photos);

    /// <summary>
    /// Creates a picker for selecting a container photo to delete.
    /// </summary>
    /// <param name="photos">The value used by the operation.</param>
    OptionPickerPopupDefinition<ImageItem> ContainerPhotoDeletePicker(IReadOnlyList<ImageItem> photos);

    /// <summary>
    /// Creates an alert stating that an item has no photos.
    /// </summary>
    AlertPopupDefinition NoItemPhotos();

    /// <summary>
    /// Creates an alert stating that a container has no photos.
    /// </summary>
    AlertPopupDefinition NoContainerPhotos();

    /// <summary>
    /// Creates a confirmation prompt for deleting an item.
    /// </summary>
    ConfirmationPopupDefinition DeleteItem();

    /// <summary>
    /// Creates a confirmation prompt for deleting an item by setting its total quantity to zero.
    /// </summary>
    /// <param name="itemName">The value used by the operation.</param>
    ConfirmationPopupDefinition DeleteItemBySettingTotalToZero(string itemName);

    /// <summary>
    /// Creates a confirmation prompt for deleting a container.
    /// </summary>
    ConfirmationPopupDefinition DeleteContainer();

    /// <summary>
    /// Creates a confirmation prompt for deleting a photo.
    /// </summary>
    ConfirmationPopupDefinition DeletePhoto();

    /// <summary>
    /// Creates a confirmation prompt for replacing an existing barcode.
    /// </summary>
    ConfirmationPopupDefinition ReplaceBarcode();

    /// <summary>
    /// Creates a confirmation prompt for clearing an existing barcode.
    /// </summary>
    ConfirmationPopupDefinition ClearBarcode();

    /// <summary>
    /// Creates a number picker for setting a container allocation quantity.
    /// </summary>
    /// <param name="initialValue">The value used by the operation.</param>
    NumberPickerPopupDefinition SetQuantity(int initialValue);

    /// <summary>
    /// Creates a number picker for setting an item's total quantity.
    /// </summary>
    /// <param name="initialValue">The value used by the operation.</param>
    /// <param name="assignedQuantity">The value used by the operation.</param>
    NumberPickerPopupDefinition SetTotalQuantity(int initialValue, int assignedQuantity);

    /// <summary>
    /// Creates a number picker for allocating unassigned item quantity.
    /// </summary>
    /// <param name="availableQuantity">The value used by the operation.</param>
    NumberPickerPopupDefinition AssociateUnassignedQuantity(int availableQuantity);

    /// <summary>
    /// Creates an alert describing an inventory quantity update failure.
    /// </summary>
    /// <param name="message">The value used by the operation.</param>
    AlertPopupDefinition InventoryQuantityUpdateFailed(string message);

    /// <summary>
    /// Creates a picker for selecting the container from which to withdraw stock.
    /// </summary>
    /// <param name="allocations">The available container allocations to present.</param>
    OptionPickerPopupDefinition<ItemContainerAllocation> WithdrawalContainerPicker(
        IReadOnlyList<ItemContainerAllocation> allocations);

    /// <summary>
    /// Creates a number picker for choosing the quantity to withdraw from a container.
    /// </summary>
    /// <param name="allocation">The selected container allocation.</param>
    /// <param name="carriedQuantity">The quantity currently carried by the user.</param>
    /// <param name="requiredQuantity">The total quantity required for the withdrawal.</param>
    NumberPickerPopupDefinition WithdrawFromContainer(
        ItemContainerAllocation allocation,
        int carriedQuantity,
        int requiredQuantity);

    /// <summary>
    /// Creates an alert explaining that the carried quantity is insufficient.
    /// </summary>
    /// <param name="carriedQuantity">The value used by the operation.</param>
    AlertPopupDefinition WithdrawalCarryTooSmall(int carriedQuantity);

    /// <summary>
    /// Creates a confirmation prompt for withdrawing unassigned quantity.
    /// </summary>
    /// <param name="unassignedQuantity">The value used by the operation.</param>
    ConfirmationPopupDefinition ConfirmUnassignedWithdrawal(int unassignedQuantity);

    /// <summary>
    /// Creates a number picker for choosing unassigned quantity to withdraw.
    /// </summary>
    /// <param name="availableQuantity">The value used by the operation.</param>
    NumberPickerPopupDefinition WithdrawUnassignedQuantity(int availableQuantity);

    /// <summary>
    /// Creates a picker for the single inventory source to consume from.
    /// </summary>
    OptionPickerPopupDefinition<ItemInventoryConsumptionSource> ConsumptionSourcePicker(
        InventorySnapshot inventory);

    /// <summary>
    /// Confirms whether consumption should use the container from which the workflow was opened.
    /// </summary>
    ConfirmationPopupDefinition ConfirmPreferredConsumptionSource(ItemContainerAllocation allocation);

    /// <summary>
    /// Creates a number picker for consuming stock from a container.
    /// </summary>
    NumberPickerPopupDefinition ConsumeFromContainer(ItemContainerAllocation allocation);

    /// <summary>
    /// Creates a number picker for consuming unassigned stock.
    /// </summary>
    NumberPickerPopupDefinition ConsumeUnassignedQuantity(int availableQuantity);

    /// <summary>
    /// Confirms deletion when consumption would exhaust all stock for an item.
    /// </summary>
    ConfirmationPopupDefinition ConfirmFinalStockConsumption(string itemName);

    /// <summary>
    /// Creates a confirmation prompt for removing an item from a container.
    /// </summary>
    /// <param name="itemName">The value used by the operation.</param>
    ConfirmationPopupDefinition RemoveItemFromContainer(string itemName);
}
