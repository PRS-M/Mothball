using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.Shared;

namespace MothballMobile.Infrastructure.Presentation.Popups;

public sealed class PopupDefinitionService : IPopupDefinitionService
{
    private static string L(string key) => LocalizationManager.Current.Get(key);

    /// <inheritdoc />
    public AlertPopupDefinition BackupExported(string fullPath)
        => new(
            L("Backup Exported"),
            LocalizationManager.Current.Format("Backup saved to:\n{0}", fullPath));

    /// <inheritdoc />
    public AlertPopupDefinition BackupExportFailed(string errorMessage)
        => new(
            L("Export Failed"),
            LocalizationManager.Current.Format("Could not export backup.\n\n{0}", errorMessage));

    /// <inheritdoc />
    public AlertPopupDefinition BackupShareFailed(string errorMessage)
        => new(
            L("Share Failed"),
            LocalizationManager.Current.Format("Could not share backup.\n\n{0}", errorMessage));

    /// <inheritdoc />
    public AlertPopupDefinition BackupSigningKeyShareFailed(string errorMessage)
        => new(
            L("Share Failed"),
            LocalizationManager.Current.Format("Could not share the backup signing key.\n\n{0}", errorMessage));

    /// <inheritdoc />
    public AlertPopupDefinition BackupSigningKeyImportFailed(string errorMessage)
        => new(
            L("Import Failed"),
            LocalizationManager.Current.Format("Could not import the backup signing key.\n\n{0}", errorMessage));

    /// <inheritdoc />
    public AlertPopupDefinition RestoreCompleted(string summary)
        => new(L("Restore Completed"), summary);

    /// <inheritdoc />
    public AlertPopupDefinition RestoreFailed(string errorMessage)
        => new(
            L("Restore Failed"),
            LocalizationManager.Current.Format("Could not import backup.\n\n{0}", errorMessage));

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteBackup(string fileName)
        => new(
            L("Delete backup"),
            LocalizationManager.Current.Format("Delete '{0}' from local backup storage?", fileName),
            L("Delete"));

    /// <inheritdoc />
    public AlertPopupDefinition BackupDeleted(string fileName)
        => new(
            L("Backup deleted"),
            LocalizationManager.Current.Format("Deleted: {0}", fileName));

    /// <inheritdoc />
    public AlertPopupDefinition DeleteBackupFailed(string errorMessage)
        => new(
            L("Delete failed"),
            LocalizationManager.Current.Format("Could not delete backup.\n\n{0}", errorMessage));

    /// <inheritdoc />
    public OptionPickerPopupDefinition<InventoryBackupConflictPolicy> RestorePolicyPicker()
        => new(
            L("Restore mode"),
            L("Cancel"),
            new[]
            {
                new PopupOption<InventoryBackupConflictPolicy>(L("Add only"), InventoryBackupConflictPolicy.AddOnly),
                new PopupOption<InventoryBackupConflictPolicy>(L("Add + upsert metadata"), InventoryBackupConflictPolicy.AddAndUpsertMetadata),
                new PopupOption<InventoryBackupConflictPolicy>(L("Full sync (roots)"), InventoryBackupConflictPolicy.FullSync),
                new PopupOption<InventoryBackupConflictPolicy>(L("Strict full sync"), InventoryBackupConflictPolicy.StrictFullSync),
            });

    /// <inheritdoc />
    public AlertPopupDefinition NoBackupsFound()
        => new(
            L("No backups found"),
            L("No backup files were found in local backup storage."));

    /// <inheritdoc />
    public OptionPickerPopupDefinition<string> BackupFilePicker(IReadOnlyList<string> fileNames)
        => new(
            L("Choose backup file"),
            L("Cancel"),
            fileNames.Select(fileName => new PopupOption<string>(fileName, fileName)).ToArray());

    /// <inheritdoc />
    public OptionPickerPopupDefinition<PhotoSource> PhotoSourcePicker()
        => new(
            L("Add photo"),
            L("Cancel"),
            new[]
            {
                new PopupOption<PhotoSource>(L("Select Photo"), PhotoSource.Library),
                new PopupOption<PhotoSource>(L("Capture Photo"), PhotoSource.Camera),
            });

    /// <inheritdoc />
    public OptionPickerPopupDefinition<ImageItem> ItemPhotoDeletePicker(IReadOnlyList<ImageItem> photos)
        => PhotoPicker(L("Choose item photo to delete"), photos);

    /// <inheritdoc />
    public OptionPickerPopupDefinition<ImageItem> ContainerPhotoDeletePicker(IReadOnlyList<ImageItem> photos)
        => PhotoPicker(L("Choose container photo to delete"), photos);

    /// <inheritdoc />
    public AlertPopupDefinition NoItemPhotos()
        => new(
            L("No photos"),
            L("This item does not have any photos to delete."));

    /// <inheritdoc />
    public AlertPopupDefinition NoContainerPhotos()
        => new(
            L("No photos"),
            L("This container does not have any photos to delete."));

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteItem()
        => new(
            L("Delete item"),
            L("Are you sure you want to delete this item? This cannot be undone."),
            L("Delete"));

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteItemBySettingTotalToZero(string itemName)
        => new(
            L("Remove item"),
            LocalizationManager.Current.Format("Setting '{0}' to zero will permanently remove the item, all container assignments, and its photos. Continue?", itemName),
            L("Remove"));

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeleteContainer()
        => new(
            L("Delete container"),
            L("Delete this container? Items inside will not be deleted, only the relation."),
            L("Delete"));

    /// <inheritdoc />
    public ConfirmationPopupDefinition DeletePhoto()
        => new(
            L("Delete photo"),
            L("Delete the selected photo?"),
            L("Delete"));

    /// <inheritdoc />
    public NumberPickerPopupDefinition SetQuantity(int initialValue)
        => new(
            L("Set quantity"),
            Min: 0,
            Max: 1000,
            InitialValue: initialValue);

    /// <inheritdoc />
    public NumberPickerPopupDefinition SetTotalQuantity(int initialValue, int assignedQuantity)
        => new(
            L("Set total quantity"),
            Min: 0,
            Max: int.MaxValue,
            InitialValue: initialValue);

    /// <inheritdoc />
    public NumberPickerPopupDefinition AssociateUnassignedQuantity(int availableQuantity)
        => new(
            L("Assign to container"),
            Min: 1,
            Max: availableQuantity,
            InitialValue: availableQuantity,
            Message: L("Enter how many unassigned items to place in this container."));

    /// <inheritdoc />
    public AlertPopupDefinition InventoryQuantityUpdateFailed(string message)
        => new(
            L("Quantity not updated"),
            message);

    /// <inheritdoc />
    public OptionPickerPopupDefinition<ItemContainerAllocation> WithdrawalContainerPicker(
        IReadOnlyList<ItemContainerAllocation> allocations)
        => new(
            L("Choose a container"),
            L("Cancel"),
            allocations
                .Where(allocation => allocation.Quantity > 0)
                .Select(allocation => new PopupOption<ItemContainerAllocation>(
                    LocalizationManager.Current.Format("{0} ({1})", allocation.ContainerName, allocation.Quantity),
                    allocation))
                .ToArray());

    /// <inheritdoc />
    public NumberPickerPopupDefinition WithdrawFromContainer(
        ItemContainerAllocation allocation,
        int carriedQuantity,
        int requiredQuantity)
        => new(
            LocalizationManager.Current.Format("Withdraw from {0}", allocation.ContainerName),
            Min: 0,
            Max: int.MaxValue,
            InitialValue: Math.Max(1, Math.Max(carriedQuantity, requiredQuantity)),
            Placeholder: L("Enter 0 to stop"),
            Message: L("Enter how many items to withdraw from this container."));

    /// <inheritdoc />
    public AlertPopupDefinition WithdrawalCarryTooSmall(int carriedQuantity)
        => new(
            L("More items must be withdrawn"),
            LocalizationManager.Current.Format("Enter 0 to cancel, or withdraw at least the remaining {0} items.", carriedQuantity));

    /// <inheritdoc />
    public ConfirmationPopupDefinition ConfirmUnassignedWithdrawal(int unassignedQuantity)
        => new(
            L("Withdraw unassigned items?"),
            LocalizationManager.Current.Format("Assigned withdrawals are complete. Continuing will withdraw from {0} unassigned items and reduce the total quantity further.", unassignedQuantity),
            L("Continue"));

    /// <inheritdoc />
    public NumberPickerPopupDefinition WithdrawUnassignedQuantity(int availableQuantity)
        => new(
            L("Withdraw unassigned items"),
            Min: 0,
            Max: availableQuantity,
            InitialValue: 0,
            Placeholder: L("Enter 0 to finish"),
            Message: L("Enter how many unassigned items to withdraw."));

    /// <inheritdoc />
    public OptionPickerPopupDefinition<ItemInventoryConsumptionSource> ConsumptionSourcePicker(
        InventorySnapshot inventory)
    {
        var options = inventory.Allocations
            .Where(allocation => allocation.Quantity > 0)
            .Select(allocation => new PopupOption<ItemInventoryConsumptionSource>(
                LocalizationManager.Current.Format("{0} ({1})", allocation.ContainerName, allocation.Quantity),
                ItemInventoryConsumptionSource.FromContainer(allocation.ContainerId)))
            .ToList();

        if (inventory.UnassignedQuantity > 0)
        {
            options.Add(new PopupOption<ItemInventoryConsumptionSource>(
                LocalizationManager.Current.Format("Unassigned stock ({0})", inventory.UnassignedQuantity),
                ItemInventoryConsumptionSource.FromUnassigned()));
        }

        return new OptionPickerPopupDefinition<ItemInventoryConsumptionSource>(
            L("Use from"),
            L("Cancel"),
            options);
    }

    /// <inheritdoc />
    public ConfirmationPopupDefinition ConfirmPreferredConsumptionSource(ItemContainerAllocation allocation)
        => new(
            L("Use from this container?"),
            LocalizationManager.Current.Format("Use '{0}' stock ({1} available)?", allocation.ContainerName, allocation.Quantity),
            L("Use here"),
            L("Choose another source"));

    /// <inheritdoc />
    public NumberPickerPopupDefinition ConsumeFromContainer(ItemContainerAllocation allocation)
        => new(
            LocalizationManager.Current.Format("Use from {0}", allocation.ContainerName),
            Min: 1,
            Max: allocation.Quantity,
            InitialValue: 1,
            Message: L("Enter how many items to use permanently."));

    /// <inheritdoc />
    public NumberPickerPopupDefinition ConsumeUnassignedQuantity(int availableQuantity)
        => new(
            L("Use unassigned stock"),
            Min: 1,
            Max: availableQuantity,
            InitialValue: 1,
            Message: L("Enter how many unassigned items to use permanently."));

    /// <inheritdoc />
    public ConfirmationPopupDefinition ConfirmFinalStockConsumption(string itemName)
        => new(
            L("Use final item?"),
            LocalizationManager.Current.Format("This will permanently remove '{0}', all assignments, and its photos. Continue?", itemName),
            L("Use and remove"));

    /// <inheritdoc />
    public ConfirmationPopupDefinition RemoveItemFromContainer(string itemName)
        => new(
            L("Remove item"),
            LocalizationManager.Current.Format("Remove '{0}' from this container? The item itself will not be deleted.", itemName),
            L("Remove"));

    private static OptionPickerPopupDefinition<ImageItem> PhotoPicker(string title, IReadOnlyList<ImageItem> photos)
        => new(
            title,
            L("Cancel"),
            photos
                .Select((photo, index) => new PopupOption<ImageItem>(LocalizationManager.Current.Format("Photo {0}", index + 1), photo))
                .ToArray());
}
