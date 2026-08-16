using CoreApp.Contracts;
using Moq;

namespace UnitTests;

[TestFixture]
public class InventoryBackupServiceTests
{
    [Test]
    public async Task ExportAndUploadAsync_ExportsThenUploads_AndReturnsExportedEnvelope()
    {
        var exporter = new Mock<IInventoryBackupExporter>(MockBehavior.Strict);
        var client = new Mock<IInventoryBackupClient>(MockBehavior.Strict);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData(),
        };

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var sequence = new MockSequence();
        exporter.InSequence(sequence)
            .Setup(e => e.ExportAsync(token))
            .ReturnsAsync(backup);
        client.InSequence(sequence)
            .Setup(c => c.UploadAsync(backup, token))
            .Returns(Task.CompletedTask);

        var service = new InventoryBackupService(exporter.Object, client.Object);

        var result = await service.ExportAndUploadAsync(token);

        Assert.That(result, Is.SameAs(backup));
        exporter.Verify(e => e.ExportAsync(token), Times.Once);
        client.Verify(c => c.UploadAsync(backup, token), Times.Once);
    }

    [Test]
    public void ExportAndUploadAsync_WhenExportFails_DoesNotUpload()
    {
        var exporter = new Mock<IInventoryBackupExporter>();
        var client = new Mock<IInventoryBackupClient>();

        exporter.Setup(e => e.ExportAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("export failed"));

        var service = new InventoryBackupService(exporter.Object, client.Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.ExportAndUploadAsync());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("export failed"));
        client.Verify(c => c.UploadAsync(It.IsAny<InventoryBackupEnvelope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ExportAndUploadAsync_WhenUploadFails_PropagatesError()
    {
        var exporter = new Mock<IInventoryBackupExporter>();
        var client = new Mock<IInventoryBackupClient>();

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData(),
        };

        exporter.Setup(e => e.ExportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(backup);
        client.Setup(c => c.UploadAsync(backup, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("upload failed"));

        var service = new InventoryBackupService(exporter.Object, client.Object);

        var ex = Assert.ThrowsAsync<IOException>(async () => await service.ExportAndUploadAsync());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("upload failed"));
        exporter.Verify(e => e.ExportAsync(It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.UploadAsync(backup, It.IsAny<CancellationToken>()), Times.Once);
    }
}
