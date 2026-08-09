using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using FluentAssertions;
using CoreApp.Interfaces;
using Moq;

namespace UnitTests;

public class ImagePathResolverTests
{
    private readonly Mock<IFileHandler> _fileHandler = new();
    private readonly IImagePathResolver _resolver;

    public ImagePathResolverTests()
    {
        _fileHandler.SetupGet(f => f.AppDataPath).Returns("/root");
        _resolver = new Infrastructure.Services.ImagePathResolver(_fileHandler.Object);
    }

    [Test]
    public void PrimaryContainerPhotoPath_Fallback_WhenNoPhotos()
    {
        var c = new Container();
        var path = _resolver.GetPrimaryContainerPhotoPath(c);
        path.Should().Be("dotnet_bot.png");
    }

    [Test]
    public void PrimaryContainerPhotoPath_UsesFirstPhoto()
    {
        var c = new Container();
        var img = c.AddImageItem();
        var expected = $"/root/{CoreApp.Utilities.Constants.PathToContainerPhotos}/{img.FileName}";
        var path = _resolver.GetPrimaryContainerPhotoPath(c);
        path.Should().Be(expected);
    }

    [Test]
    public void GetItemPhotoPaths_ReturnsAllOrFallback()
    {
        var item = new Item();
        // fallback case
        var single = _resolver.GetItemPhotoPaths(item).Single();
        single.Should().Be("dotnet_bot.png");

        // add photos
        var img1 = item.AddImageItem();
        var img2 = item.AddImageItem();
        var list = _resolver.GetItemPhotoPaths(item).ToList();
        list.Should().BeEquivalentTo(new[] {
            $"/root/{CoreApp.Utilities.Constants.PathToItemPhotos}/{img1.FileName}",
            $"/root/{CoreApp.Utilities.Constants.PathToItemPhotos}/{img2.FileName}" });
    }

    [Test]
    public void BuildPath_SwallowsExceptions_ReturnsFallback()
    {
        var badMock = new Mock<IFileHandler>();
        badMock.SetupGet(f => f.AppDataPath).Throws(new Exception("nope"));
        var badResolver = new Infrastructure.Services.ImagePathResolver(badMock.Object);
        var item = new Item();
        item.AddImageItem();
        var path = badResolver.GetPrimaryItemPhotoPath(item);
        path.Should().Be("dotnet_bot.png");
    }
}
