using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Restore.Planning;

internal static class InventoryBackupPayloadShapeValidator
{
    public static void ValidatePayloadShape(InventoryBackupEnvelope backup)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);

        foreach (var container in backup.Data.Containers)
        {
            if (container.ContainerId == Guid.Empty)
            {
                throw new InvalidDataException("Backup container ID cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(container.Name))
            {
                throw new InvalidDataException("Backup container name cannot be empty.");
            }
        }

        foreach (var item in backup.Data.Items)
        {
            if (item.ItemId == Guid.Empty)
            {
                throw new InvalidDataException("Backup item ID cannot be empty.");
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

            if (string.IsNullOrWhiteSpace(image.FileName))
            {
                throw new InvalidDataException("Backup image file name cannot be empty.");
            }
        }
    }
}
