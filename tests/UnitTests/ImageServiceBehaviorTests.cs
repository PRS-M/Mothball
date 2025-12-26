using System;
using System.Threading.Tasks;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Utilities;
using Moq;

namespace UnitTests;

[TestFixture]
public class ImageServiceBehaviorTests
{
    [Test]
    public async Task CaptureContainerPhotoAsync_SavesBytes_AddsImage_AndPersists()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryDomainRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {1,2,3};
        camera.Setup(c => c.CapturePhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos, bytes))
             .ReturnsAsync("/fake/path");

        var service = new ImageService(camera.Object, repo.Object, files.Object);
        var container = new Container(Guid.NewGuid(), "Box", "notes");

        await service.CaptureContainerPhotoAsync(container);

        Assert.That(container.Photos.Count, Is.EqualTo(1));
        var image = container.Photos[0];
        files.Verify(f => f.SaveFileAsync(image.FileName, Constants.PathToContainerPhotos, bytes), Times.Once);
        repo.Verify(r => r.InsertImageItemAsync(image, container.ContainerId), Times.Once);
        repo.Verify(r => r.UpdateContainerAsync(container), Times.Once);
    }

    [Test]
    public void CaptureContainerPhotoAsync_SaveFails_RollsBackImage()
    {
        var camera = new Mock<ICameraHandler>();
        var repo = new Mock<IInventoryDomainRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {1};
        camera.Setup(c => c.CapturePhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToContainerPhotos, bytes))
             .ThrowsAsync(new IOException("disk full"));

        var service = new ImageService(camera.Object, repo.Object, files.Object);
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
        var repo = new Mock<IInventoryDomainRepository>();
        var files = new Mock<IFileHandler>();

        var bytes = new byte[] {9,8};
        camera.Setup(c => c.CapturePhotoAsync()).ReturnsAsync(bytes);
        files.Setup(f => f.SaveFileAsync(It.IsAny<string>(), Constants.PathToItemPhotos, bytes))
             .ReturnsAsync("/fake/path");

        var service = new ImageService(camera.Object, repo.Object, files.Object);
        var item = new Item { Name = "Hat" };

        await service.CaptureItemPhotoAsync(item);

        Assert.That(item.Photos.Count, Is.EqualTo(1));
        var image = item.Photos[0];
        files.Verify(f => f.SaveFileAsync(image.FileName, Constants.PathToItemPhotos, bytes), Times.Once);
        repo.Verify(r => r.InsertImageItemAsync(image, item.ItemId), Times.Once);
        repo.Verify(r => r.UpdateItemAsync(item), Times.Once);
    }
}
