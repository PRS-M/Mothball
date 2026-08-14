using CoreApp.Contracts;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace MothballMobile.Infrastructure.Popups;

public sealed class PopupDefinitionService : IPopupDefinitionService
{
    public AlertPopupDefinition BackupExported(string fullPath)
        => new(
            "Backup Exported",
            $"Backup saved to:\n{fullPath}");

    public AlertPopupDefinition BackupExportFailed(string errorMessage)
        => new(
            "Export Failed",
            $"Could not export backup to JSON.\n\n{errorMessage}");

    public AlertPopupDefinition RestoreCompleted(string summary)
        => new("Restore Completed", summary);

    public AlertPopupDefinition RestoreFailed(string errorMessage)
        => new(
            "Restore Failed",
            $"Could not import backup from JSON.\n\n{errorMessage}");

    public ConfirmationPopupDefinition DeleteBackup(string fileName)
        => new(
            "Delete backup",
            $"Delete '{fileName}' from local backup storage?",
            "Delete");

    public AlertPopupDefinition BackupDeleted(string fileName)
        => new(
            "Backup deleted",
            $"Deleted: {fileName}");

    public AlertPopupDefinition DeleteBackupFailed(string errorMessage)
        => new(
            "Delete failed",
            $"Could not delete backup JSON.\n\n{errorMessage}");

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

    public AlertPopupDefinition NoBackupsFound()
        => new(
            "No backups found",
            "No JSON backup files were found in local backup storage.");

    public OptionPickerPopupDefinition<string> BackupFilePicker(IReadOnlyList<string> fileNames)
        => new(
            "Choose backup file",
            "Cancel",
            fileNames.Select(fileName => new PopupOption<string>(fileName, fileName)).ToArray());

    public OptionPickerPopupDefinition<PhotoSource> PhotoSourcePicker()
        => new(
            "Add photo",
            "Cancel",
            new[]
            {
                new PopupOption<PhotoSource>("Select Photo", PhotoSource.Library),
                new PopupOption<PhotoSource>("Capture Photo", PhotoSource.Camera),
            });

    public OptionPickerPopupDefinition<ImageItem> ItemPhotoDeletePicker(IReadOnlyList<ImageItem> photos)
        => PhotoPicker("Choose item photo to delete", photos);

    public OptionPickerPopupDefinition<ImageItem> ContainerPhotoDeletePicker(IReadOnlyList<ImageItem> photos)
        => PhotoPicker("Choose container photo to delete", photos);

    public AlertPopupDefinition NoItemPhotos()
        => new(
            "No photos",
            "This item does not have any photos to delete.");

    public AlertPopupDefinition NoContainerPhotos()
        => new(
            "No photos",
            "This container does not have any photos to delete.");

    public ConfirmationPopupDefinition DeleteItem()
        => new(
            "Delete item",
            "Are you sure you want to delete this item? This cannot be undone.",
            "Delete");

    public ConfirmationPopupDefinition DeleteContainer()
        => new(
            "Delete container",
            "Delete this container? Items inside will not be deleted, only the relation.",
            "Delete");

    public ConfirmationPopupDefinition DeletePhoto()
        => new(
            "Delete photo",
            "Delete the selected photo?",
            "Delete");

    public NumberPickerPopupDefinition SetQuantity(int initialValue)
        => new(
            "Set quantity",
            Min: 0,
            Max: 1000,
            InitialValue: initialValue);

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
