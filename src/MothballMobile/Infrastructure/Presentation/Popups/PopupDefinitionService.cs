using CoreApp.Entities.Inventory;
using CoreApp.Contracts;
using CoreApp.Entities.Shared;

namespace MothballMobile.Infrastructure.Presentation.Popups;

public sealed class PopupDefinitionService : IPopupDefinitionService
{
    /// <inheritdoc />
    public AlertPopupDefinition BackupExported(string fullPath)
        => new(
            "Backup Exported",
            $"Backup saved to:\n{fullPath}");

    /// <inheritdoc />
    public AlertPopupDefinition BackupExportFailed(string errorMessage)
        => new(
            "Export Failed",
            $"Could not export backup.\n\n{errorMessage}");

    /// <inheritdoc />
    public AlertPopupDefinition BackupShareFailed(string errorMessage)
        => new(
            "Share Failed",
            $"Could not share backup.\n\n{errorMessage}");

    /// <inheritdoc />
    public AlertPopupDefinition RestoreCompleted(string summary)
        => new("Restore Completed", summary);

    /// <inheritdoc />
    public AlertPopupDefinition RestoreFailed(string errorMessage)
        => new(
            "Restore Failed",
            $"Could not import backup.\n\n{errorMessage}");

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteBackup(string fileName)
        => new(
            "Delete backup",
            $"Delete '{fileName}' from local backup storage?",
            "Delete");

    /// <inheritdoc />
    public AlertPopupDefinition BackupDeleted(string fileName)
        => new(
            "Backup deleted",
            $"Deleted: {fileName}");

    /// <inheritdoc />
    public AlertPopupDefinition DeleteBackupFailed(string errorMessage)
        => new(
            "Delete failed",
            $"Could not delete backup.\n\n{errorMessage}");

    /// <inheritdoc />
    public OptionPickerPopupDefinition<InventoryBackupConflictPolicy> RestorePolicyPicker()
        => new(
            "Restore mode",
            "Cancel",
            new[]
            {
                new PopupOption<InventoryBackupConflictPolicy>("Add only", InventoryBackupConflictPolicy.AddOnly),
                new PopupOption<InventoryBackupConflictPolicy>("Add + upsert metadata", InventoryBackupConflictPolicy.AddAndUpsertMetadata),
                new PopupOption<InventoryBackupConflictPolicy>("Full sync (roots)", InventoryBackupConflictPolicy.FullSync),
                new PopupOption<InventoryBackupConflictPolicy>("Strict full sync", InventoryBackupConflictPolicy.StrictFullSync),
            });

    /// <inheritdoc />
    public AlertPopupDefinition NoBackupsFound()
        => new(
            "No backups found",
            "No backup files were found in local backup storage.");

    /// <inheritdoc />
    public OptionPickerPopupDefinition<string> BackupFilePicker(IReadOnlyList<string> fileNames)
        => new(
            "Choose backup file",
            "Cancel",
            fileNames.Select(fileName => new PopupOption<string>(fileName, fileName)).ToArray());

    /// <inheritdoc />
    public OptionPickerPopupDefinition<PhotoSource> PhotoSourcePicker()
        => new(
            "Add photo",
            "Cancel",
            new[]
            {
                new PopupOption<PhotoSource>("Select Photo", PhotoSource.Library),
                new PopupOption<PhotoSource>("Capture Photo", PhotoSource.Camera),
            });

    /// <inheritdoc />
    public OptionPickerPopupDefinition<ImageItem> ItemPhotoDeletePicker(IReadOnlyList<ImageItem> photos)
        => PhotoPicker("Choose item photo to delete", photos);

    /// <inheritdoc />
    public OptionPickerPopupDefinition<ImageItem> ContainerPhotoDeletePicker(IReadOnlyList<ImageItem> photos)
        => PhotoPicker("Choose container photo to delete", photos);

    /// <inheritdoc />
    public AlertPopupDefinition NoItemPhotos()
        => new(
            "No photos",
            "This item does not have any photos to delete.");

    /// <inheritdoc />
    public AlertPopupDefinition NoContainerPhotos()
        => new(
            "No photos",
            "This container does not have any photos to delete.");

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteItem()
        => new(
            "Delete item",
            "Are you sure you want to delete this item? This cannot be undone.",
            "Delete");

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteItemBySettingTotalToZero(string itemName)
        => new(
            "Remove item",
            $"Setting '{itemName}' to zero will permanently remove the item, all container assignments, and its photos. Continue?",
            "Remove");

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteContainer()
        => new(
            "Delete container",
            "Delete this container? Items inside will not be deleted, only the relation.",
            "Delete");

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeletePhoto()
        => new(
            "Delete photo",
            "Delete the selected photo?",
            "Delete");

    /// <inheritdoc />
    public NumberPickerPopupDefinition SetQuantity(int initialValue)
        => new(
            "Set quantity",
            Min: 0,
            Max: 1000,
            InitialValue: initialValue);

    /// <inheritdoc />
    public NumberPickerPopupDefinition SetTotalQuantity(int initialValue, int assignedQuantity)
        => new(
            "Set total quantity",
            Min: 0,
            Max: int.MaxValue,
            InitialValue: initialValue);

    /// <inheritdoc />
    public NumberPickerPopupDefinition AssociateUnassignedQuantity(int availableQuantity)
        => new(
            "Assign to container",
            Min: 1,
            Max: availableQuantity,
            InitialValue: availableQuantity,
            Message: "Enter how many unassigned items to place in this container.");

    /// <inheritdoc />
    public AlertPopupDefinition InventoryQuantityUpdateFailed(string message)
        => new(
            "Quantity not updated",
            message);

    /// <inheritdoc />
    public OptionPickerPopupDefinition<ItemContainerAllocation> WithdrawalContainerPicker(
        IReadOnlyList<ItemContainerAllocation> allocations)
        => new(
            "Choose a container",
            "Cancel",
            allocations
                .Where(allocation => allocation.Quantity > 0)
                .Select(allocation => new PopupOption<ItemContainerAllocation>(
                    $"{allocation.ContainerName} ({allocation.Quantity})",
                    allocation))
                .ToArray());

    /// <inheritdoc />
    public NumberPickerPopupDefinition WithdrawFromContainer(
        ItemContainerAllocation allocation,
        int carriedQuantity,
        int requiredQuantity)
        => new(
            $"Withdraw from {allocation.ContainerName}",
            Min: 0,
            Max: int.MaxValue,
            InitialValue: Math.Max(1, Math.Max(carriedQuantity, requiredQuantity)),
            Placeholder: "Enter 0 to stop",
            Message: "Enter how many items to withdraw from this container.");

    /// <inheritdoc />
    public AlertPopupDefinition WithdrawalCarryTooSmall(int carriedQuantity)
        => new(
            "More items must be withdrawn",
            $"Enter 0 to cancel, or withdraw at least the remaining {carriedQuantity} items.");

    /// <inheritdoc />
    public ConfirmationPopupDefinition ConfirmUnassignedWithdrawal(int unassignedQuantity)
        => new(
            "Withdraw unassigned items?",
            $"Assigned withdrawals are complete. Continuing will withdraw from {unassignedQuantity} unassigned items and reduce the total quantity further.",
            "Continue");

    /// <inheritdoc />
    public NumberPickerPopupDefinition WithdrawUnassignedQuantity(int availableQuantity)
        => new(
            "Withdraw unassigned items",
            Min: 0,
            Max: availableQuantity,
            InitialValue: 0,
            Placeholder: "Enter 0 to finish",
            Message: "Enter how many unassigned items to withdraw.");

    /// <inheritdoc />
    public ConfirmationPopupDefinition RemoveItemFromContainer(string itemName)
        => new(
            "Remove item",
            $"Remove '{itemName}' from this container? The item itself will not be deleted.",
            "Remove");

    private static OptionPickerPopupDefinition<ImageItem> PhotoPicker(string title, IReadOnlyList<ImageItem> photos)
        => new(
            title,
            "Cancel",
            photos
                .Select((photo, index) => new PopupOption<ImageItem>($"Photo {index + 1}", photo))
                .ToArray());
}
