using CoreApp.Domain.Entities.ContainerAggregate;
using Moq;
using MothballMobile.UI.Features.Containers.ContainersList;

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
    public async Task SearchCommand_WithQuery_ReplacesListWithFilteredResults()
    {
        var queries = new Mock<IContainerListQueryHandler>();
        queries.Setup(q => q.QueryAsync(false, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(false, "Box", null, null))
            .ReturnsAsync([new Container(Guid.NewGuid(), "Box", "Notes")]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        viewModel.Query = "Box";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.That(viewModel.Containers, Has.Count.EqualTo(1));
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

    private static ContainerListViewModel CreateViewModel(IContainerListQueryHandler queries)
    {
        var paths = new Mock<IImagePathResolver>();
        paths.Setup(p => p.GetContainerPhotoPaths(It.IsAny<Container>())).Returns(Array.Empty<string>());

        return new ContainerListViewModel(
            paths.Object,
            queries,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(),
            Mock.Of<IBackgroundTaskObserver>());
    }
}
