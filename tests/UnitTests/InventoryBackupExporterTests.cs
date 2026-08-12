using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Services;
using Moq;

namespace UnitTests;

[TestFixture]
public class InventoryBackupExporterTests
{
    [Test]
    public async Task ExportAsync_ProducesExpectedSnapshotShape()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Shelf A");
        var item = new Item
        {
            ItemId = Guid.NewGuid(),
            Name = "Zip Ties",
            Description = "Black 8 inch",
        };

        container.AddItem(item.ItemId, 5);
        container.AddImageItem(Guid.NewGuid());
        item.Photos.Add(new ImageItem(Guid.NewGuid()));

        var queries = new Mock<IInventoryQueryRepository>();
        queries
            .Setup(q => q.QueryContainersAsync(It.IsAny<CoreApp.Specifications.ContainerListSpecification>()))
            .ReturnsAsync([container]);
        queries
            .Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<CoreApp.Specifications.ItemListSpecification>()))
            .ReturnsAsync([item]);

        var sut = new InventoryBackupExporter(queries.Object);

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
        });
    }

    [Test]
    public async Task ExportAsJsonAsync_UsesCamelCaseContract()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        queries
            .Setup(q => q.QueryContainersAsync(It.IsAny<CoreApp.Specifications.ContainerListSpecification>()))
            .ReturnsAsync(new List<Container>());
        queries
            .Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<CoreApp.Specifications.ItemListSpecification>()))
            .ReturnsAsync(new List<Item>());

        var sut = new InventoryBackupExporter(queries.Object);

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
}
