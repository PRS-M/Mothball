using System.IO.Compression;
using CoreApp.Contracts;
using CoreApp.Features.Backup.Archive;
using CoreApp.Features.Backup.Restore.Planning;
using CoreApp.Utilities;

namespace CoreApp.Features.Backup.Restore;

public sealed class InventoryBackupZipRestoreService : IInventoryBackupZipRestoreService
{
    private readonly IInventoryBackupRestoreService backupRestoreService;
    private readonly IFileHandler fileHandler;

    public InventoryBackupZipRestoreService(
        IInventoryBackupRestoreService backupRestoreService,
        IFileHandler fileHandler)
    {
        this.backupRestoreService = backupRestoreService ?? throw new ArgumentNullException(nameof(backupRestoreService));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
    }

    /// <inheritdoc />
    public async Task<InventoryBackupZipRestoreResult> RestoreFromZipAsync(
        byte[] backupZip,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backupZip);

        using var zipStream = new MemoryStream(backupZip);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var backupJsonEntry = archive.GetEntry(InventoryBackupZipArchive.BackupJsonEntryName)
            ?? throw new InvalidOperationException("The ZIP backup does not contain backup.json.");

        string backupJson;
        await using (var entryStream = backupJsonEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            backupJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        var backup = InventoryBackupRestorePlanner.ParseBackupJson(backupJson);
        var result = await backupRestoreService
            .RestoreAsync(backup, options, cancellationToken)
            .ConfigureAwait(false);

        var restoredPhotoFiles = await RestorePhotoFilesAsync(archive, backup, cancellationToken)
            .ConfigureAwait(false);

        return new InventoryBackupZipRestoreResult(result, restoredPhotoFiles);
    }

    private async Task<int> RestorePhotoFilesAsync(
        ZipArchive archive,
        InventoryBackupEnvelope backup,
        CancellationToken cancellationToken)
    {
        var containerIds = backup.Data.Containers.Select(c => c.ContainerId).ToHashSet();
        var itemIds = backup.Data.Items.Select(i => i.ItemId).ToHashSet();
        var restoredEntryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var restored = 0;

        foreach (var image in backup.Data.Images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!BackupContainsImageOwner(image, containerIds, itemIds))
            {
                continue;
            }

            var fileName = Path.GetFileName(image.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var entryPath = InventoryBackupZipArchive.GetPhotoEntryPath(image.OwnerType, fileName);
            if (!restoredEntryPaths.Add(entryPath))
            {
                continue;
            }

            var entry = archive.GetEntry(entryPath);
            if (entry is null)
            {
                continue;
            }

            await using var entryStream = entry.Open();
            await using var photoStream = new MemoryStream();
            await entryStream.CopyToAsync(photoStream, cancellationToken).ConfigureAwait(false);
            await fileHandler
                .SaveFileAsync(fileName, InventoryBackupZipArchive.GetPhotoFolder(image.OwnerType), photoStream.ToArray())
                .ConfigureAwait(false);

            restored++;
        }

        return restored;
    }

    private static bool BackupContainsImageOwner(
        InventoryBackupImageRef image,
        HashSet<Guid> containerIds,
        HashSet<Guid> itemIds)
        => image.OwnerType == InventoryBackupOwnerType.Container
            ? containerIds.Contains(image.OwnerId)
            : itemIds.Contains(image.OwnerId);
}
