using CoreApp.Interfaces;
using CoreApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests;

[TestFixture]
public class ServiceTests
{
    [Test]
    public async Task ImageService_CaptureContainerPhotoAsync_ThrowsOnNull()
    {
        var cameraMock = new Mock<ICameraHandler>();
        var repoMock = new Mock<IInventoryCommandRepository>();
        var fileHandlerMock = new Mock<IFileHandler>();
        cameraMock.Setup(c => c.CapturePhotoAsync()).ReturnsAsync(Array.Empty<byte>());
        var service = new ImageService(cameraMock.Object, repoMock.Object, fileHandlerMock.Object, NullLogger<ImageService>.Instance);
        Assert.ThrowsAsync<ArgumentNullException>(() => service.CaptureContainerPhotoAsync(null!));
    }

    [Test]
    public async Task JsonHandler_SerializeToFile_ThrowsOnNullFileName()
    {
        var fileHandlerMock = new Mock<IFileHandler>();
        var handler = new JsonHandler(fileHandlerMock.Object);
        Assert.ThrowsAsync<ArgumentNullException>(() => handler.SerializeToFile<object>(null!, "folder", new object()));
    }
}
