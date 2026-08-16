using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CoreApp.Contracts;
using CoreApp.Utilities;
using Moq;

namespace UnitTests;

[TestFixture]
public class InventoryBackupZipRestoreServiceTests
{
    [Test]
    public async Task RestoreFromZipAsync_RestoresMetadataAndPhotoFiles()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var containerPhotoId = Guid.NewGuid();
        var itemPhotoId = Guid.NewGuid();
        byte[] containerPhotoBytes = [1, 2, 3];
        byte[] itemPhotoBytes = [4, 5, 6];

        var backup = CreateBackup(containerId, itemId, containerPhotoId, itemPhotoId);
        var zipBytes = CreateZip(
            backup,
            ($"images/containers/{containerPhotoId}.jpg", containerPhotoBytes),
            ($"images/items/{itemPhotoId}.jpg", itemPhotoBytes));

        var restoreResult = new InventoryBackupRestoreResult
        {
            AddedContainers = 1,
            AddedItems = 1,
            AddedImages = 2,
        };

        var restoreService = new Mock<IInventoryBackupRestoreService>();
        restoreService
            .Setup(s => s.RestoreAsync(
                It.Is<InventoryBackupEnvelope>(b => b.Data.Images.Count == 2),
                It.IsAny<InventoryBackupRestoreOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(restoreResult);

        var fileHandler = new Mock<IFileHandler>();
        fileHandler
            .Setup(f => f.SaveFileAsync(
                $"{containerPhotoId}.jpg",
                Constants.PathToContainerPhotos,
                It.Is<byte[]>(bytes => bytes.SequenceEqual(containerPhotoBytes))))
            .ReturnsAsync("/tmp/container.jpg");
        fileHandler
            .Setup(f => f.SaveFileAsync(
                $"{itemPhotoId}.jpg",
                Constants.PathToItemPhotos,
                It.Is<byte[]>(bytes => bytes.SequenceEqual(itemPhotoBytes))))
            .ReturnsAsync("/tmp/item.jpg");

        var sut = new InventoryBackupZipRestoreService(restoreService.Object, fileHandler.Object);

        var result = await sut.RestoreFromZipAsync(zipBytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.SameAs(restoreResult));
            Assert.That(result.RestoredPhotoFiles, Is.EqualTo(2));
        });

        fileHandler.VerifyAll();
    }

    [Test]
    public async Task RestoreFromZipAsync_SkipsPhotoEntryWithoutBackupOwner()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var validPhotoId = Guid.NewGuid();
        var orphanPhotoId = Guid.NewGuid();

        var backup = CreateBackup(containerId, itemId, validPhotoId, itemPhotoId: null);
        backup = backup with
        {
            Data = backup.Data with
            {
                Images =
                [
                    .. backup.Data.Images,
                    new InventoryBackupImageRef
                    {
                        ImageId = orphanPhotoId,
                        OwnerId = Guid.NewGuid(),
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{orphanPhotoId}.jpg",
                    },
                ],
            },
        };
        backup = InventoryBackupRestorePlanner.AttachIntegrity(backup);

        var zipBytes = CreateZip(
            backup,
            ($"images/containers/{validPhotoId}.jpg", [1, 2, 3]),
            ($"images/containers/{orphanPhotoId}.jpg", [4, 5, 6]));

        var restoreService = new Mock<IInventoryBackupRestoreService>();
        restoreService
            .Setup(s => s.RestoreAsync(It.IsAny<InventoryBackupEnvelope>(), It.IsAny<InventoryBackupRestoreOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryBackupRestoreResult());

        var fileHandler = new Mock<IFileHandler>();
        fileHandler
            .Setup(f => f.SaveFileAsync($"{validPhotoId}.jpg", Constants.PathToContainerPhotos, It.IsAny<byte[]>()))
            .ReturnsAsync("/tmp/valid.jpg");

        var sut = new InventoryBackupZipRestoreService(restoreService.Object, fileHandler.Object);

        var result = await sut.RestoreFromZipAsync(zipBytes);

        Assert.That(result.RestoredPhotoFiles, Is.EqualTo(1));
        fileHandler.Verify(f => f.SaveFileAsync($"{orphanPhotoId}.jpg", It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public void RestoreFromZipAsync_ThrowsWhenBackupJsonIsMissing()
    {
        var zipBytes = CreateZipWithoutBackupJson(("images/items/photo.jpg", [1, 2, 3]));

        var restoreService = new Mock<IInventoryBackupRestoreService>();
        var fileHandler = new Mock<IFileHandler>();
        var sut = new InventoryBackupZipRestoreService(restoreService.Object, fileHandler.Object);

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.RestoreFromZipAsync(zipBytes));
        restoreService.Verify(s => s.RestoreAsync(It.IsAny<InventoryBackupEnvelope>(), It.IsAny<InventoryBackupRestoreOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static InventoryBackupEnvelope CreateBackup(
        Guid containerId,
        Guid itemId,
        Guid? containerPhotoId,
        Guid? itemPhotoId)
    {
        var images = new List<InventoryBackupImageRef>();

        if (containerPhotoId is Guid containerImageId)
        {
            images.Add(new InventoryBackupImageRef
            {
                ImageId = containerImageId,
                OwnerId = containerId,
                OwnerType = InventoryBackupOwnerType.Container,
                FileName = $"{containerImageId}.jpg",
            });
        }

        if (itemPhotoId is Guid itemImageId)
        {
            images.Add(new InventoryBackupImageRef
            {
                ImageId = itemImageId,
                OwnerId = itemId,
                OwnerType = InventoryBackupOwnerType.Item,
                FileName = $"{itemImageId}.jpg",
            });
        }

        return InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer
                    {
                        ContainerId = containerId,
                        Name = "Garage",
                        Notes = "Shelf A",
                    },
                ],
                Items =
                [
                    new InventoryBackupItem
                    {
                        ItemId = itemId,
                        Name = "Zip Ties",
                        Description = "Black 8 inch",
                    },
                ],
                Images = images,
            },
        });
    }

    private static byte[] CreateZip(
        InventoryBackupEnvelope backup,
        params (string EntryName, byte[] Bytes)[] entries)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var backupEntry = archive.CreateEntry("backup.json");
            using (var stream = backupEntry.Open())
            {
                var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
                var bytes = Encoding.UTF8.GetBytes(json);
                stream.Write(bytes);
            }

            AddEntries(archive, entries);
        }

        return zipStream.ToArray();
    }

    private static byte[] CreateZipWithoutBackupJson(params (string EntryName, byte[] Bytes)[] entries)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntries(archive, entries);
        }

        return zipStream.ToArray();
    }

    private static void AddEntries(
        ZipArchive archive,
        params (string EntryName, byte[] Bytes)[] entries)
    {
        foreach (var (entryName, bytes) in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var stream = entry.Open();
            stream.Write(bytes);
        }
    }
}
