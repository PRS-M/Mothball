using CoreApp.Contracts;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace MothballMobile.Infrastructure.Popups;

public interface IPopupDefinitionService
{
    AlertPopupDefinition BackupExported(string fullPath);

    AlertPopupDefinition BackupExportFailed(string errorMessage);

    AlertPopupDefinition RestoreCompleted(string summary);

    AlertPopupDefinition RestoreFailed(string errorMessage);

    ConfirmationPopupDefinition DeleteBackup(string fileName);

    AlertPopupDefinition BackupDeleted(string fileName);

    AlertPopupDefinition DeleteBackupFailed(string errorMessage);

    OptionPickerPopupDefinition<InventoryBackupConflictPolicy> RestorePolicyPicker();

    AlertPopupDefinition NoBackupsFound();

    OptionPickerPopupDefinition<string> BackupFilePicker(IReadOnlyList<string> fileNames);

    OptionPickerPopupDefinition<PhotoSource> PhotoSourcePicker();

    OptionPickerPopupDefinition<ImageItem> ItemPhotoDeletePicker(IReadOnlyList<ImageItem> photos);

    OptionPickerPopupDefinition<ImageItem> ContainerPhotoDeletePicker(IReadOnlyList<ImageItem> photos);

    AlertPopupDefinition NoItemPhotos();

    AlertPopupDefinition NoContainerPhotos();

    ConfirmationPopupDefinition DeleteItem();

    ConfirmationPopupDefinition DeleteContainer();

    ConfirmationPopupDefinition DeletePhoto();

    NumberPickerPopupDefinition SetQuantity(int initialValue);

    ConfirmationPopupDefinition RemoveItemFromContainer(string itemName);
}
