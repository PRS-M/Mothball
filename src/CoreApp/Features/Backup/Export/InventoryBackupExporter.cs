using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CoreApp.Contracts;
using CoreApp.Features.Backup.Archive;
using CoreApp.Features.Backup.Restore.Planning;
using CoreApp.Specifications;
using CoreApp.Utilities;

namespace CoreApp.Features.Backup.Export;

public sealed class InventoryBackupExporter : IInventoryBackupExporter
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly IFileHandler fileHandler;

    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public InventoryBackupExporter(
        IInventoryQueryRepository inventoryQueries,
        IFileHandler fileHandler)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
    }

    /// <inheritdoc />
    public async Task<InventoryBackupEnvelope> ExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var containers = await inventoryQueries
            .QueryContainersAsync(new ContainerListSpecification(ContainerQueryFilter.All))
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var items = await inventoryQueries
            .QueryItemsWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))
            .ConfigureAwait(false);
        var inventorySnapshots = await inventoryQueries
            .QueryInventorySnapshotsAsync(new ItemListSpecification(ItemQueryFilter.All))
            .ConfigureAwait(false) ?? [];

        var backupContainers = containers
            .Select(c => new InventoryBackupContainer
            {
                ContainerId = c.ContainerId,
                Name = c.Name,
                Notes = c.Notes,
            })
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.ContainerId)
            .ToList();

        var backupItems = inventorySnapshots
            .Select(i => new InventoryBackupItem
            {
                ItemId = i.Item.ItemId,
                Name = i.Item.Name,
                Description = i.Item.Description,
                TotalQuantity = i.TotalQuantity,
            })
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ItemId)
            .ToList();

        var backupRelations = inventorySnapshots
            .SelectMany(snapshot => snapshot.Allocations.Select(allocation => new InventoryBackupRelation
            {
                ContainerId = allocation.ContainerId,
                ItemId = snapshot.Item.ItemId,
                Quantity = allocation.Quantity,
            }))
            .Where(r => r.Quantity > 0)
            .OrderBy(r => r.ContainerId)
            .ThenBy(r => r.ItemId)
            .ToList();

        var backupImages = containers
            .SelectMany(c => c.Photos.Select(photo => new InventoryBackupImageRef
            {
                ImageId = photo.ImageId,
                OwnerId = c.ContainerId,
                OwnerType = InventoryBackupOwnerType.Container,
                FileName = photo.FileName,
            }))
            .Concat(items.SelectMany(i => i.Photos.Select(photo => new InventoryBackupImageRef
            {
                ImageId = photo.ImageId,
                OwnerId = i.ItemId,
                OwnerType = InventoryBackupOwnerType.Item,
                FileName = photo.FileName,
            })))
            .OrderBy(i => i.OwnerType)
            .ThenBy(i => i.OwnerId)
            .ThenBy(i => i.ImageId)
            .ToList();

        var backup = new InventoryBackupEnvelope
        {
            CreatedUtc = DateTimeOffset.UtcNow,
            Data = new InventoryBackupData
            {
                Containers = backupContainers,
                Items = backupItems,
                Relations = backupRelations,
                Images = backupImages,
            },
        };

        return InventoryBackupRestorePlanner.AttachIntegrity(backup);
    }

    /// <inheritdoc />
    public async Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default)
    {
        var backup = await ExportAsync(cancellationToken).ConfigureAwait(false);
        return SerializeBackup(backup);
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportAsZipAsync(CancellationToken cancellationToken = default)
    {
        var backup = await ExportAsync(cancellationToken).ConfigureAwait(false);

        await using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteJsonEntryAsync(archive, backup, cancellationToken).ConfigureAwait(false);
            await WritePhotoEntriesAsync(archive, backup.Data.Images, cancellationToken).ConfigureAwait(false);
        }

        return zipStream.ToArray();
    }

    private static string SerializeBackup(InventoryBackupEnvelope backup)
        => JsonSerializer.Serialize(backup, BackupJsonOptions);

    private static async Task WriteJsonEntryAsync(
        ZipArchive archive,
        InventoryBackupEnvelope backup,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(InventoryBackupZipArchive.BackupJsonEntryName, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteAsync(SerializeBackup(backup).AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private async Task WritePhotoEntriesAsync(
        ZipArchive archive,
        IEnumerable<InventoryBackupImageRef> images,
        CancellationToken cancellationToken)
    {
        var addedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var image in images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(image.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var entryPath = InventoryBackupZipArchive.GetPhotoEntryPath(image.OwnerType, fileName);
            if (!addedEntries.Add(entryPath))
            {
                continue;
            }

            byte[] bytes;
            try
            {
                bytes = await fileHandler
                    .ReadFileAsync(fileName, InventoryBackupZipArchive.GetPhotoFolder(image.OwnerType))
                    .ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            await using var stream = entry.Open();
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
    }
}
