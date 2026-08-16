using CoreApp.Entities.Inventory;
using CoreApp.Contracts;
using CoreApp.Entities.Shared;

namespace MothballMobile.Infrastructure.Presentation.Popups;

public interface IPopupDefinitionService
{
    AlertPopupDefinition BackupExported(string fullPath);

    AlertPopupDefinition BackupExportFailed(string errorMessage);

    AlertPopupDefinition BackupShareFailed(string errorMessage);

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

    ConfirmationPopupDefinition DeleteItemBySettingTotalToZero(string itemName);

    ConfirmationPopupDefinition DeleteContainer();

    ConfirmationPopupDefinition DeletePhoto();

    NumberPickerPopupDefinition SetQuantity(int initialValue);

    NumberPickerPopupDefinition SetTotalQuantity(int initialValue, int assignedQuantity);

    NumberPickerPopupDefinition AssociateUnassignedQuantity(int availableQuantity);

    AlertPopupDefinition InventoryQuantityUpdateFailed(string message);

    OptionPickerPopupDefinition<ItemContainerAllocation> WithdrawalContainerPicker(
        IReadOnlyList<ItemContainerAllocation> allocations);

    NumberPickerPopupDefinition WithdrawFromContainer(
        ItemContainerAllocation allocation,
        int carriedQuantity,
        int requiredQuantity);

    AlertPopupDefinition WithdrawalCarryTooSmall(int carriedQuantity);

    ConfirmationPopupDefinition ConfirmUnassignedWithdrawal(int unassignedQuantity);

    NumberPickerPopupDefinition WithdrawUnassignedQuantity(int availableQuantity);

    ConfirmationPopupDefinition RemoveItemFromContainer(string itemName);
}
