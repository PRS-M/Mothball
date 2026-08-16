using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests;

[TestFixture]
public class ServiceTests
{
    [Test]
    public async Task ImageService_CaptureContainerPhotoAsync_ThrowsOnNull()
    {
        var repoMock = new Mock<IInventoryCommandRepository>();

        var service = new ImageService(
            Mock.Of<IPhotoSourceReader>(),
            Mock.Of<IPhotoFilePersistenceService>(),
            Mock.Of<ITemporaryPhotoService>(),
            Mock.Of<IPhotoDeletionService>(),
            repoMock.Object);

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
