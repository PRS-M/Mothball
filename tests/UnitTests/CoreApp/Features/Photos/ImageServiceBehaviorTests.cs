using System;
using System.Threading.Tasks;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Entities.Shared;
using CoreApp.Application.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Mothball.Tests.Unit.Core.Features.Photos;

[TestFixture]
public class ImageServiceBehaviorTests
{
    private static ImageService CreateService(
        Mock<ICameraHandler> camera,
        Mock<IInventoryCommandRepository> repo,
        Mock<IFileHandler> files)
    {
        var photoSourceReader = new PhotoSourceReader(camera.Object);
        return new ImageService(
            photoSourceReader,
            new PhotoFilePersistenceService(files.Object, NullLogger<PhotoFilePersistenceService>.Instance),
            new TemporaryPhotoService(photoSourceReader, files.Object, NullLogger<TemporaryPhotoService>.Instance),
            new PhotoDeletionService(repo.Object, files.Object, NullLogger<PhotoDeletionService>.Instance),
            repo.Object);
    }

    [Test]
    public async Task CaptureContainerPhotoAsync_SavesBytes_AddsImage_AndPersists()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {1,2,3};
        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos, bytes))
             .ReturnsAsync("/fake/path");

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "notes");

        await service.CaptureContainerPhotoAsync(container);

        Assert.That(container.Photos.Count, Is.EqualTo(1));
        var image = container.Photos[0];
        files.Verify(f => f.SaveFileAsync(image.FileName, Constants.PathToContainerPhotos, bytes), Times.Once);
        repo.Verify(r => r.InsertImageItemAsync(image, container.ContainerId), Times.Once);
        repo.Verify(r => r.UpdateContainerAsync(container), Times.Once);
    }

    [Test]
    public async Task CaptureItemPhotoAsync_WithCameraSource_CapturesNewPhoto()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] { 4, 2 };
        camera.Setup(c => c.CapturePhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToItemPhotos, bytes))
             .ReturnsAsync("/fake/path");

        var service = CreateService(camera, repo, files);
        var item = new Item("Hat", string.Empty);

        await service.CaptureItemPhotoAsync(item, source: PhotoSource.Camera);

        Assert.That(item.Photos.Count, Is.EqualTo(1));
        camera.Verify(c => c.CapturePhotoAsync(), Times.Once);
        camera.Verify(c => c.SelectPhotoAsync(), Times.Never);
    }

    [Test]
    public async Task CaptureContainerPhotoAsync_SaveFails_RollsBackImage()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {1};
        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos, bytes))
             .ThrowsAsync(new IOException("disk full"));

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "notes");

        Assert.ThrowsAsync<IOException>(() => service.CaptureContainerPhotoAsync(container));
        Assert.That(container.Photos.Count, Is.EqualTo(0));
        repo.Verify(r => r.InsertImageItemAsync(It.IsAny<ImageItem>(), It.IsAny<Guid>()), Times.Never);
        repo.Verify(r => r.UpdateContainerAsync(It.IsAny<Container>()), Times.Never);
    }

    [Test]
    public async Task CaptureItemPhotoAsync_SavesBytes_AddsImage_AndPersists()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {9,8};
        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToItemPhotos, bytes))
             .ReturnsAsync("/fake/path");

        var service = CreateService(camera, repo, files);
        var item = new Item("Hat", string.Empty);

        await service.CaptureItemPhotoAsync(item);

        Assert.That(item.Photos.Count, Is.EqualTo(1));
        var image = item.Photos[0];
        files.Verify(f => f.SaveFileAsync(image.FileName, Constants.PathToItemPhotos, bytes), Times.Once);
        repo.Verify(r => r.InsertImageItemAsync(image, item.ItemId), Times.Once);
        repo.Verify(r => r.UpdateItemAsync(item), Times.Once);
    }

    [Test]
    public async Task CaptureContainerPhotoAsync_PersistFails_RollsBackImage_AndDeletesFile()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {4, 5};
        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos, bytes))
             .ReturnsAsync("/fake/path");
        repo.Setup(r => r.InsertImageItemAsync(It.IsAny<ImageItem>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("db write failed"));

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "notes");

        Assert.ThrowsAsync<InvalidOperationException>(() => service.CaptureContainerPhotoAsync(container));
        Assert.That(container.Photos.Count, Is.EqualTo(0));

        files.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos), Times.Once);
        repo.Verify(r => r.UpdateContainerAsync(It.IsAny<Container>()), Times.Never);
    }

    [Test]
    public async Task CaptureItemPhotoAsync_PersistFails_RollsBackImage_AndDeletesFile()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {7, 6};
        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToItemPhotos, bytes))
             .ReturnsAsync("/fake/path");
        repo.Setup(r => r.InsertImageItemAsync(It.IsAny<ImageItem>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("db write failed"));

        var service = CreateService(camera, repo, files);
        var item = new Item("Hat", string.Empty);

        Assert.ThrowsAsync<InvalidOperationException>(() => service.CaptureItemPhotoAsync(item));
        Assert.That(item.Photos.Count, Is.EqualTo(0));

        files.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), Constants.PathToItemPhotos), Times.Once);
        repo.Verify(r => r.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    [Test]
    public async Task CaptureTemporaryPhotoAsync_SavesToTemporaryFolder_AndReturnsDescriptor()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {10, 11, 12};
        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToTemporaryPhotos, bytes))
             .ReturnsAsync("/tmp/temp-photo.jpg");

        var service = CreateService(camera, repo, files);

        var result = await service.CaptureTemporaryPhotoAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Bytes, Is.EqualTo(bytes));
        Assert.That(result.FullPath, Is.EqualTo("/tmp/temp-photo.jpg"));
        Assert.That(result.FolderPath, Is.EqualTo(Constants.PathToTemporaryPhotos));
        Assert.That(result.FileName, Does.StartWith("temp-"));

        files.Verify(f => f.SaveFileAsync(result.FileName, Constants.PathToTemporaryPhotos, bytes), Times.Once);
    }

    [Test]
    public async Task CaptureTemporaryPhotoAsync_WhenCameraReturnsEmpty_ReturnsNull_AndDoesNotSave()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(Array.Empty<byte>());

        var service = CreateService(camera, repo, files);

        var result = await service.CaptureTemporaryPhotoAsync();

        Assert.That(result, Is.Null);
        files.Verify(f => f.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task DeleteTemporaryPhotoAsync_WithBlankFileName_DoesNothing()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();
        var service = CreateService(camera, repo, files);

        await service.DeleteTemporaryPhotoAsync("   ");

        files.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteTemporaryPhotoAsync_WhenFileMissing_SwallowsNotFound()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        files.Setup(f => f.DeleteFileAsync("temp.jpg", Constants.PathToTemporaryPhotos))
            .ThrowsAsync(new FileNotFoundException());

        var service = CreateService(camera, repo, files);

        Assert.DoesNotThrowAsync(async () => await service.DeleteTemporaryPhotoAsync("temp.jpg"));
        files.Verify(f => f.DeleteFileAsync("temp.jpg", Constants.PathToTemporaryPhotos), Times.Once);
    }

    [Test]
    public async Task DeleteTemporaryPhotoAsync_WithValidFile_DeletesOnce()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        files.Setup(f => f.DeleteFileAsync("temp.jpg", Constants.PathToTemporaryPhotos))
            .Returns(Task.CompletedTask);

        var service = CreateService(camera, repo, files);

        await service.DeleteTemporaryPhotoAsync("temp.jpg");

        files.Verify(f => f.DeleteFileAsync("temp.jpg", Constants.PathToTemporaryPhotos), Times.Once);
    }

    [Test]
    public async Task CaptureItemPhotoAsync_WhenCameraReturnsEmptyBytes_ReturnsZero_AndDoesNotPersist()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        camera.Setup(c => c.SelectPhotoAsync()).ReturnsAsync(Array.Empty<byte>());

        var service = CreateService(camera, repo, files);
        var item = new Item("Hat", string.Empty);

        var saved = await service.CaptureItemPhotoAsync(item);

        Assert.That(saved, Is.EqualTo(0));
        Assert.That(item.Photos, Is.Empty);
        files.Verify(f => f.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        repo.Verify(r => r.InsertImageItemAsync(It.IsAny<ImageItem>(), It.IsAny<Guid>()), Times.Never);
        repo.Verify(r => r.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    [Test]
    public async Task SaveContainerPhotoAsync_UsesProvidedBytes_AndPersistsPhoto()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {22, 11};
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos, bytes))
             .ReturnsAsync("/fake/path");

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "N");

        await service.SaveContainerPhotoAsync(container, bytes);

        Assert.That(container.Photos.Count, Is.EqualTo(1));
        var image = container.Photos[0];
        files.Verify(f => f.SaveFileAsync(image.FileName, Constants.PathToContainerPhotos, bytes), Times.Once);
        repo.Verify(r => r.InsertImageItemAsync(image, container.ContainerId), Times.Once);
        repo.Verify(r => r.UpdateContainerAsync(container), Times.Once);
    }

    [Test]
    public async Task SaveContainerPhotoAsync_WhenPersistAndCleanupDeleteFail_RethrowsOriginalError()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {6, 7};
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos, bytes))
             .ReturnsAsync("/fake/path");
        repo.Setup(r => r.InsertImageItemAsync(It.IsAny<ImageItem>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("persist failed"));
        files.Setup(f => f.DeleteFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos))
            .ThrowsAsync(new IOException("cleanup failed"));

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "N");

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.SaveContainerPhotoAsync(container, bytes));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("persist failed"));
        Assert.That(container.Photos, Is.Empty);
        repo.Verify(r => r.UpdateContainerAsync(It.IsAny<Container>()), Times.Never);
    }

    [Test]
    public async Task SaveItemPhotoAsync_UsesProvidedBytes_AndPersistsPhoto()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {33, 44};
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToItemPhotos, bytes))
             .ReturnsAsync("/fake/path");

        var service = CreateService(camera, repo, files);
        var item = new Item("Lamp", string.Empty);

        await service.SaveItemPhotoAsync(item, bytes);

        Assert.That(item.Photos.Count, Is.EqualTo(1));
        var image = item.Photos[0];
        files.Verify(f => f.SaveFileAsync(image.FileName, Constants.PathToItemPhotos, bytes), Times.Once);
        repo.Verify(r => r.InsertImageItemAsync(image, item.ItemId), Times.Once);
        repo.Verify(r => r.UpdateItemAsync(item), Times.Once);
    }

    [Test]
    public async Task DeleteContainerPhotoAsync_WhenPhotoExists_DeletesMetadataAndFile()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "N");
        var image = container.AddImageItem();

        var deleted = await service.DeleteContainerPhotoAsync(container, image.ImageId);

        Assert.That(deleted, Is.True);
        Assert.That(container.Photos, Is.Empty);
        repo.Verify(r => r.DeleteContainerPhotoAsync(container, image.ImageId), Times.Once);
        repo.Verify(r => r.UpdateContainerAsync(It.IsAny<Container>()), Times.Never);
        files.Verify(f => f.DeleteFileAsync(image.FileName, Constants.PathToContainerPhotos), Times.Once);
    }

    [Test]
    public async Task DeleteContainerPhotoAsync_WhenPhotoDoesNotExist_ReturnsFalse_AndDoesNothing()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "N");

        var deleted = await service.DeleteContainerPhotoAsync(container, Guid.NewGuid());

        Assert.That(deleted, Is.False);
        repo.Verify(r => r.DeleteContainerPhotoAsync(It.IsAny<Container>(), It.IsAny<Guid>()), Times.Never);
        repo.Verify(r => r.UpdateContainerAsync(It.IsAny<Container>()), Times.Never);
        files.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void DeleteContainerPhotoAsync_WhenRepositoryDeleteFails_RestoresInMemoryPhoto_AndRethrows()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var service = CreateService(camera, repo, files);
        var container = new Container(Guid.NewGuid(), "Box", "N");
        var image = container.AddImageItem();

        repo.Setup(r => r.DeleteContainerPhotoAsync(container, image.ImageId))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.DeleteContainerPhotoAsync(container, image.ImageId));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("delete failed"));
        Assert.That(container.Photos.Count, Is.EqualTo(1));
        Assert.That(container.Photos[0].ImageId, Is.EqualTo(image.ImageId));
        files.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteItemPhotoAsync_WhenPhotoExists_DeletesMetadataAndFile()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryCommandRepository>();
        var files = new Mock<IFileHandler>();

        var service = CreateService(camera, repo, files);
        var item = new Item("Lamp", string.Empty);
        var image = item.AddImageItem();

        var deleted = await service.DeleteItemPhotoAsync(item, image.ImageId);

        Assert.That(deleted, Is.True);
        Assert.That(item.Photos, Is.Empty);
        repo.Verify(r => r.DeleteItemPhotoAsync(item, image.ImageId), Times.Once);
        repo.Verify(r => r.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
        files.Verify(f => f.DeleteFileAsync(image.FileName, Constants.PathToItemPhotos), Times.Once);
    }
}
