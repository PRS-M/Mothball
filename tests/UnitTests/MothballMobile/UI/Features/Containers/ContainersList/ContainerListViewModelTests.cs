using CoreApp.Domain.Entities.ContainerAggregate;
using Moq;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.UI.Features.Containers.ContainersList;
using MothballMobile.Infrastructure.BarcodeDocuments;
using CoreApp.Domain.Entities.Shared;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Containers.ContainersList;

[TestFixture]
public sealed class ContainerListViewModelTests
{
    [Test]
    public async Task InitializeAsync_LoadsFirstPage()
    {
        var containers = new List<Container> { new(Guid.NewGuid(), "Box", "Notes") };
        var queries = new Mock<IContainerListQueryHandler>();
        queries.Setup(q => q.QueryAsync(false, null, 0, 10)).ReturnsAsync(containers);
        var viewModel = CreateViewModel(queries.Object);

        await viewModel.InitializeAsync();

        Assert.That(viewModel.Containers, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task InitializeAsync_PublishesRowsWithImagePathsAlreadyPopulated()
    {
        var container = new Container(Guid.NewGuid(), "Box", "Notes");
        container.AddImageItem();
        var queries = new Mock<IContainerListQueryHandler>();
        queries.Setup(q => q.QueryAsync(false, null, 0, 10)).ReturnsAsync([container]);
        var paths = new Mock<IImagePathResolver>();
        paths.Setup(p => p.GetContainerPhotoPaths(container)).Returns(["box.jpg"]);
        var viewModel = CreateViewModel(queries.Object, paths.Object);
        var publishedWithImage = false;
        viewModel.Containers.CollectionChanged += (_, args) =>
            publishedWithImage = args.NewItems?[0] is ContainerViewModel row
                && row.ImagePaths.SequenceEqual(["box.jpg"]);

        await viewModel.InitializeAsync();

        Assert.That(publishedWithImage, Is.True);
    }

    [Test]
    public async Task SearchCommand_WithQuery_ReplacesListWithFilteredResults()
    {
        var queries = new Mock<IContainerListQueryHandler>();
        queries.Setup(q => q.QueryAsync(false, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(false, "Box", 0, 10))
            .ReturnsAsync([new Container(Guid.NewGuid(), "Box", "Notes")]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        viewModel.Query = "Box";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.That(viewModel.Containers, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LoadNextPageCommand_DuringSearch_AppendsNextFilteredPage()
    {
        var firstPage = Enumerable.Range(1, 10)
            .Select(index => new Container(Guid.NewGuid(), $"Box {index}", "Notes"))
            .ToList();
        var lastContainer = new Container(Guid.NewGuid(), "Box 11", "Notes");
        var queries = new Mock<IContainerListQueryHandler>();
        queries.Setup(q => q.QueryAsync(false, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(false, "Box", 0, 10)).ReturnsAsync(firstPage);
        queries.Setup(q => q.QueryAsync(false, "Box", 1, 10)).ReturnsAsync([lastContainer]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        viewModel.Query = "Box";
        await viewModel.SearchCommand.ExecuteAsync(null);
        await viewModel.LoadNextPageCommand.ExecuteAsync(null);

        Assert.That(viewModel.Containers.Select(container => container.Name),
            Is.EqualTo(firstPage.Select(container => container.Name).Append(lastContainer.Name)));
    }

    [Test]
    public async Task RefreshCommand_ReloadsFirstPage()
    {
        var queries = new Mock<IContainerListQueryHandler>();
        queries.SetupSequence(q => q.QueryAsync(false, null, 0, 10))
            .ReturnsAsync([])
            .ReturnsAsync([new Container(Guid.NewGuid(), "Box", "Notes")]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.That(viewModel.Containers, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ShareAllMatchingCommand_QueriesUnpagedContainersAndSharesBarcodes()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Notes");
        container.UpdateBarcode(new Barcode("GARAGE-01", BarcodeSymbology.Code128));
        var queries = new Mock<IContainerListQueryHandler>();
        queries.Setup(q => q.QueryAsync(false, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(false, "garage", null, null)).ReturnsAsync([container]);
        var share = new Mock<IBarcodeShareService>();
        var viewModel = CreateViewModel(queries.Object, barcodeShare: share.Object);
        await viewModel.InitializeAsync();
        viewModel.Query = "garage";

        await viewModel.ShareAllMatchingCommand.ExecuteAsync(null);

        queries.Verify(q => q.QueryAsync(false, "garage", null, null), Times.Once);
        share.Verify(service => service.ShareAsync(
            It.Is<IReadOnlyCollection<BarcodeLabelData>>(labels => labels.Single().BarcodeValue == "GARAGE-01"),
            "Share container barcodes"), Times.Once);
    }

    private static ContainerListViewModel CreateViewModel(
        IContainerListQueryHandler queries,
        IImagePathResolver? paths = null,
        IBarcodeShareService? barcodeShare = null)
    {
        if (paths is null)
        {
            var pathMock = new Mock<IImagePathResolver>();
            pathMock.Setup(resolver => resolver.GetContainerPhotoPaths(It.IsAny<Container>()))
                .Returns(Array.Empty<string>());
            paths = pathMock.Object;
        }

        return new ContainerListViewModel(
            paths,
            queries,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(),
            Mock.Of<IInventoryChangeTracker>(),
            new BarcodeLookupCoordinator(
                Mock.Of<IBarcodeScanSession>(),
                Mock.Of<IInventoryQueryRepository>(),
                Mock.Of<INavigationService>()),
            Mock.Of<IBackgroundTaskObserver>(),
            barcodeShare: barcodeShare);
    }
}
