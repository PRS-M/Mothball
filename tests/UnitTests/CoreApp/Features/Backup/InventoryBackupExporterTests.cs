using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Contracts;
using CoreApp.Application.Utilities;
using Moq;
using System.IO.Compression;

namespace Mothball.Tests.Unit.Core.Features.Backup;

[TestFixture]
public class InventoryBackupExporterTests
{
    [Test]
    public async Task ExportAsync_ProducesExpectedSnapshotShape()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Shelf A");
        var item = new Item("Zip Ties", "Black 8 inch");
        container.UpdateBarcode(new Barcode("CONTAINER-42", BarcodeSymbology.Code128));
        item.UpdateBarcode(new Barcode("1234567890123", BarcodeSymbology.Ean13));

        container.AddImageItem(Guid.NewGuid());
        item.AddImageItem(Guid.NewGuid());

        var queries = new Mock<IInventoryQueryRepository>();
        queries
            .Setup(q => q.QueryContainersAsync(It.IsAny<CoreApp.Application.Specifications.ContainerListSpecification>()))
            .ReturnsAsync([container]);
        queries
            .Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([item]);
        queries
            .Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([
                new InventorySnapshot(
                    item,
                    5,
                    5,
                    [new ItemContainerAllocation(container.ContainerId, container.Name, 5)])
            ]);

        var sut = new InventoryBackupExporter(queries.Object, Mock.Of<IFileHandler>());

        var backup = await sut.ExportAsync();

        Assert.Multiple(() =>
        {
            Assert.That(backup.PayloadVersion, Is.EqualTo(1));
            Assert.That(backup.Integrity.PayloadChecksum, Is.Not.Empty);
            Assert.That(backup.Integrity.ChecksumAlgorithm, Is.EqualTo("SHA256"));
            Assert.That(backup.Data.Containers.Count, Is.EqualTo(1));
            Assert.That(backup.Data.Items.Count, Is.EqualTo(1));
            Assert.That(backup.Data.Relations.Count, Is.EqualTo(1));
            Assert.That(backup.Data.Images.Count, Is.EqualTo(2));
            Assert.That(backup.Data.Relations[0].Quantity, Is.EqualTo(5));
            Assert.That(backup.Data.Containers[0].BarcodeValue, Is.EqualTo("CONTAINER-42"));
            Assert.That(backup.Data.Containers[0].BarcodeSymbology, Is.EqualTo((int)BarcodeSymbology.Code128));
            Assert.That(backup.Data.Items[0].BarcodeValue, Is.EqualTo("1234567890123"));
            Assert.That(backup.Data.Items[0].BarcodeSymbology, Is.EqualTo((int)BarcodeSymbology.Ean13));
        });
    }

    [Test]
    public async Task ExportAsJsonAsync_UsesCamelCaseContract()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        queries
            .Setup(q => q.QueryContainersAsync(It.IsAny<CoreApp.Application.Specifications.ContainerListSpecification>()))
            .ReturnsAsync(new List<Container>());
        queries
            .Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync(new List<Item>());
        queries
            .Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync(new List<InventorySnapshot>());

        var sut = new InventoryBackupExporter(queries.Object, Mock.Of<IFileHandler>());

        string json = await sut.ExportAsJsonAsync();

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("payloadVersion"));
            Assert.That(json, Does.Contain("schemaVersion"));
            Assert.That(json, Does.Contain("createdUtc"));
            Assert.That(json, Does.Contain("integrity"));
            Assert.That(json, Does.Contain("payloadChecksum"));
            Assert.That(json, Does.Contain("data"));
        });
    }

    [Test]
    public async Task ExportAsZipAsync_IncludesJsonAndPhotoFiles()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Shelf A");
        var item = new Item("Zip Ties", "Black 8 inch");
        var containerPhoto = container.AddImageItem(Guid.NewGuid());
        var itemPhoto = item.AddImageItem(Guid.NewGuid());

        var queries = new Mock<IInventoryQueryRepository>();
        queries
            .Setup(q => q.QueryContainersAsync(It.IsAny<CoreApp.Application.Specifications.ContainerListSpecification>()))
            .ReturnsAsync([container]);
        queries
            .Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([item]);
        queries
            .Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);

        var fileHandler = new Mock<IFileHandler>();
        fileHandler
            .Setup(f => f.ReadFileAsync(containerPhoto.FileName, Constants.PathToContainerPhotos))
            .ReturnsAsync([1, 2, 3]);
        fileHandler
            .Setup(f => f.ReadFileAsync(itemPhoto.FileName, Constants.PathToItemPhotos))
            .ReturnsAsync([4, 5, 6]);

        var sut = new InventoryBackupExporter(queries.Object, fileHandler.Object);

        byte[] zipBytes = await sut.ExportAsZipAsync();

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        Assert.Multiple(() =>
        {
            Assert.That(archive.GetEntry("backup.json"), Is.Not.Null);
            Assert.That(archive.GetEntry($"images/containers/{containerPhoto.FileName}"), Is.Not.Null);
            Assert.That(archive.GetEntry($"images/items/{itemPhoto.FileName}"), Is.Not.Null);
        });
    }

    [Test]
    public async Task ExportAsZipAsync_WithSignatureSecret_SignsEmbeddedBackupJson()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        queries
            .Setup(q => q.QueryContainersAsync(It.IsAny<CoreApp.Application.Specifications.ContainerListSpecification>()))
            .ReturnsAsync([]);
        queries
            .Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([]);
        queries
            .Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([]);

        var sut = new InventoryBackupExporter(queries.Object, Mock.Of<IFileHandler>());

        byte[] zipBytes = await sut.ExportAsZipAsync("test-signature-secret", "device-key");

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var backupJsonEntry = archive.GetEntry("backup.json");
        Assert.That(backupJsonEntry, Is.Not.Null);

        await using var entryStream = backupJsonEntry!.Open();
        using var reader = new StreamReader(entryStream);
        var backup = InventoryBackupRestorePlanner.ParseBackupJson(await reader.ReadToEndAsync());

        Assert.Multiple(() =>
        {
            Assert.That(backup.Integrity.SignatureAlgorithm, Is.EqualTo("HMAC-SHA256"));
            Assert.That(backup.Integrity.KeyId, Is.EqualTo("device-key"));
            Assert.DoesNotThrow(() => InventoryBackupRestorePlanner.ValidateIntegrity(
                backup,
                new InventoryBackupRestoreOptions { SignatureSecret = "test-signature-secret" }));
        });
    }

    [Test]
    public async Task ExportAsZipAsync_SkipsMissingPhotoFiles()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Shelf A");
        var missingPhoto = container.AddImageItem(Guid.NewGuid());

        var queries = new Mock<IInventoryQueryRepository>();
        queries
            .Setup(q => q.QueryContainersAsync(It.IsAny<CoreApp.Application.Specifications.ContainerListSpecification>()))
            .ReturnsAsync([container]);
        queries
            .Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([]);
        queries
            .Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<CoreApp.Application.Specifications.ItemListSpecification>()))
            .ReturnsAsync([]);

        var fileHandler = new Mock<IFileHandler>();
        fileHandler
            .Setup(f => f.ReadFileAsync(missingPhoto.FileName, Constants.PathToContainerPhotos))
            .ThrowsAsync(new FileNotFoundException());

        var sut = new InventoryBackupExporter(queries.Object, fileHandler.Object);

        byte[] zipBytes = await sut.ExportAsZipAsync();

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        Assert.Multiple(() =>
        {
            Assert.That(archive.GetEntry("backup.json"), Is.Not.Null);
            Assert.That(archive.GetEntry($"images/containers/{missingPhoto.FileName}"), Is.Null);
        });
    }
}
