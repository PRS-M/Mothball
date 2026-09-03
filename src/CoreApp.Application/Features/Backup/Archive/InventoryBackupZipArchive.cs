using CoreApp.Application.Utilities;

namespace CoreApp.Application.Features.Backup.Archive;

internal static class InventoryBackupZipArchive
{
    public const string BackupJsonEntryName = "backup.json";

    public static string GetPhotoFolder(InventoryBackupOwnerType ownerType)
        => ownerType == InventoryBackupOwnerType.Container
            ? Constants.PathToContainerPhotos
            : Constants.PathToItemPhotos;

    public static string GetPhotoEntryPath(InventoryBackupOwnerType ownerType, string fileName)
        => ownerType == InventoryBackupOwnerType.Container
            ? $"images/containers/{fileName}"
            : $"images/items/{fileName}";
}
