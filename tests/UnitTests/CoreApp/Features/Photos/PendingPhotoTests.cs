using CoreApp.Application.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Mothball.Tests.Unit.Core.Features.Photos;

[TestFixture]
public class PendingPhotoTests
{
    private static ImageService CreateImageService(Mock<IFileHandler> files, Mock<ICameraHandler> camera)
    {
        var photoSourceReader = new PhotoSourceReader(camera.Object);
        return new ImageService(
            photoSourceReader,
            Mock.Of<IPhotoFilePersistenceService>(),
            new TemporaryPhotoService(photoSourceReader, files.Object, NullLogger<TemporaryPhotoService>.Instance),
            Mock.Of<IPhotoDeletionService>(),
            Mock.Of<IInventoryCommandRepository>());
    }

    [Test]
    public async Task CaptureAsync_WhenSourceReturnsBytes_StagesPhotoAndReturnsTrue()
    {
        var files = new Mock<IFileHandler>();
        var camera = new Mock<ICameraHandler>();
        camera.Setup(c => c.CapturePhotoAsync(It.IsAny<IProgress<double>?>())).ReturnsAsync([1, 2, 3]);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToTemporaryPhotos, It.IsAny<byte[]>()))
            .ReturnsAsync((string name, string folder, byte[] _) => $"/tmp/{folder}/{name}");
        var pendingPhoto = new PendingPhoto(CreateImageService(files, camera));

        var captured = await pendingPhoto.CaptureAsync(PhotoSource.Camera);

        Assert.Multiple(() =>
        {
            Assert.That(captured, Is.True);
            Assert.That(pendingPhoto.HasPhoto, Is.True);
            Assert.That(pendingPhoto.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(pendingPhoto.FullPath, Does.Contain("/tmp/"));
        });
    }

    [Test]
    public async Task CaptureAsync_WhenSourceReturnsNoBytes_LeavesNoPendingPhoto()
    {
        var files = new Mock<IFileHandler>();
        var camera = new Mock<ICameraHandler>();
        camera.Setup(c => c.CapturePhotoAsync(It.IsAny<IProgress<double>?>())).ReturnsAsync([]);
        var pendingPhoto = new PendingPhoto(CreateImageService(files, camera));

        var captured = await pendingPhoto.CaptureAsync(PhotoSource.Camera);

        Assert.Multiple(() =>
        {
            Assert.That(captured, Is.False);
            Assert.That(pendingPhoto.HasPhoto, Is.False);
        });
        files.Verify(f => f.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task CaptureAsync_WhenReplacingExistingCapture_DeletesThePreviousTempFile()
    {
        var files = new Mock<IFileHandler>();
        var camera = new Mock<ICameraHandler>();
        var callCount = 0;
        camera.Setup(c => c.CapturePhotoAsync(It.IsAny<IProgress<double>?>()))
            .ReturnsAsync(() => callCount++ == 0 ? new byte[] { 1 } : [2]);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToTemporaryPhotos, It.IsAny<byte[]>()))
            .ReturnsAsync((string name, string folder, byte[] _) => $"/tmp/{name}");
        var pendingPhoto = new PendingPhoto(CreateImageService(files, camera));

        await pendingPhoto.CaptureAsync(PhotoSource.Camera);
        var firstFileName = Path.GetFileName(pendingPhoto.FullPath);
        await pendingPhoto.CaptureAsync(PhotoSource.Camera);

        Assert.That(pendingPhoto.Bytes, Is.EqualTo(new byte[] { 2 }));
        files.Verify(f => f.DeleteFileAsync(firstFileName!, Constants.PathToTemporaryPhotos), Times.Once);
    }

    [Test]
    public async Task DiscardAsync_WhenPhotoStaged_DeletesTempFileAndClearsState()
    {
        var files = new Mock<IFileHandler>();
        var camera = new Mock<ICameraHandler>();
        camera.Setup(c => c.CapturePhotoAsync(It.IsAny<IProgress<double>?>())).ReturnsAsync([1]);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToTemporaryPhotos, It.IsAny<byte[]>()))
            .ReturnsAsync((string name, string folder, byte[] _) => $"/tmp/{name}");
        var pendingPhoto = new PendingPhoto(CreateImageService(files, camera));
        await pendingPhoto.CaptureAsync(PhotoSource.Camera);
        var fileName = Path.GetFileName(pendingPhoto.FullPath);

        await pendingPhoto.DiscardAsync();

        Assert.Multiple(() =>
        {
            Assert.That(pendingPhoto.HasPhoto, Is.False);
            Assert.That(pendingPhoto.Bytes, Is.Null);
        });
        files.Verify(f => f.DeleteFileAsync(fileName!, Constants.PathToTemporaryPhotos), Times.Once);
    }

    [Test]
    public async Task DiscardAsync_WhenNoPhotoStaged_DoesNotAttemptDelete()
    {
        var files = new Mock<IFileHandler>();
        var camera = new Mock<ICameraHandler>();
        var pendingPhoto = new PendingPhoto(CreateImageService(files, camera));

        await pendingPhoto.DiscardAsync();

        files.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
