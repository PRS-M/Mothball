using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Restore.Planning;

internal static class InventoryBackupPayloadShapeValidator
{
    public static void ValidatePayloadShape(InventoryBackupEnvelope backup)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);

        var containerIds = new HashSet<Guid>();
        foreach (var container in backup.Data.Containers)
        {
            if (container.ContainerId == Guid.Empty)
            {
                throw new InvalidDataException("Backup container ID cannot be empty.");
            }

            if (!containerIds.Add(container.ContainerId))
            {
                throw new InvalidDataException("Backup container IDs must be unique.");
            }

            if (string.IsNullOrWhiteSpace(container.Name))
            {
                throw new InvalidDataException("Backup container name cannot be empty.");
            }
        }

        var itemIds = new HashSet<Guid>();
        foreach (var item in backup.Data.Items)
        {
            if (item.ItemId == Guid.Empty)
            {
                throw new InvalidDataException("Backup item ID cannot be empty.");
            }

            if (!itemIds.Add(item.ItemId))
            {
                throw new InvalidDataException("Backup item IDs must be unique.");
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                throw new InvalidDataException("Backup item name cannot be empty.");
            }

            if (item.TotalQuantity < 1)
            {
                throw new InvalidDataException("Backup item total quantity must be at least one.");
            }
        }

        foreach (var relation in backup.Data.Relations)
        {
            if (relation.ContainerId == Guid.Empty)
            {
                throw new InvalidDataException("Backup relation container ID cannot be empty.");
            }

            if (relation.ItemId == Guid.Empty)
            {
                throw new InvalidDataException("Backup relation item ID cannot be empty.");
            }

            if (relation.Quantity <= 0)
            {
                throw new InvalidDataException("Backup relation quantity must be positive.");
            }
        }

        var imageKeys = new HashSet<(InventoryBackupOwnerType OwnerType, Guid OwnerId, Guid ImageId)>();
        foreach (var image in backup.Data.Images)
        {
            if (image.ImageId == Guid.Empty)
            {
                throw new InvalidDataException("Backup image ID cannot be empty.");
            }

            if (image.OwnerId == Guid.Empty)
            {
                throw new InvalidDataException("Backup image owner ID cannot be empty.");
            }

            if (image.OwnerType == InventoryBackupOwnerType.Unknown || !Enum.IsDefined(image.OwnerType))
            {
                throw new InvalidDataException("Backup image owner type is invalid.");
            }

            if (!imageKeys.Add((image.OwnerType, image.OwnerId, image.ImageId)))
            {
                throw new InvalidDataException("Backup image ownership entries cannot be duplicated.");
            }

            if (string.IsNullOrWhiteSpace(image.FileName))
            {
                throw new InvalidDataException("Backup image file name cannot be empty.");
            }
        }
    }
}
