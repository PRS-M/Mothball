using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Mothball.Tests.Unit.Infrastructure.Services.Images;

public class ImagePathResolverTests
{
    private readonly Mock<IFileHandler> _fileHandler = new();
    private readonly IImagePathResolver _resolver;

    public ImagePathResolverTests()
    {
        _fileHandler.SetupGet(f => f.AppDataPath).Returns("/root");
        _resolver = new ImagePathResolver(_fileHandler.Object, NullLogger<ImagePathResolver>.Instance);
    }

    [Test]
    public void PrimaryContainerPhotoPath_Fallback_WhenNoPhotos()
    {
        var c = new Container();
        var path = _resolver.GetPrimaryContainerPhotoPath(c);
        Assert.That(path, Is.EqualTo("mothball_logo.png"));
    }

    [Test]
    public void PrimaryContainerPhotoPath_UsesFirstPhoto()
    {
        var c = new Container();
        var img = c.AddImageItem();
        var expected = $"/root/{CoreApp.Application.Utilities.Constants.PathToContainerPhotos}/{img.FileName}";
        var path = _resolver.GetPrimaryContainerPhotoPath(c);
        Assert.That(path, Is.EqualTo(expected));
    }

    [Test]
    public void GetItemPhotoPaths_ReturnsAllOrFallback()
    {
        var item = new Item();
        // fallback case
        var single = _resolver.GetItemPhotoPaths(item).Single();
        Assert.That(single, Is.EqualTo("mothball_logo.png"));

        // add photos
        var img1 = item.AddImageItem();
        var img2 = item.AddImageItem();
        var list = _resolver.GetItemPhotoPaths(item).ToList();
        Assert.That(list, Is.EquivalentTo(new[] {
            $"/root/{CoreApp.Application.Utilities.Constants.PathToItemPhotos}/{img1.FileName}",
            $"/root/{CoreApp.Application.Utilities.Constants.PathToItemPhotos}/{img2.FileName}" }));
    }

    [Test]
    public void BuildPath_SwallowsExceptions_ReturnsFallback()
    {
        var badMock = new Mock<IFileHandler>();
        badMock.SetupGet(f => f.AppDataPath).Throws(new Exception("nope"));
        var badResolver = new ImagePathResolver(badMock.Object, NullLogger<ImagePathResolver>.Instance);
        var item = new Item();
        item.AddImageItem();
        var path = badResolver.GetPrimaryItemPhotoPath(item);
        Assert.That(path, Is.EqualTo("mothball_logo.png"));
    }
}
