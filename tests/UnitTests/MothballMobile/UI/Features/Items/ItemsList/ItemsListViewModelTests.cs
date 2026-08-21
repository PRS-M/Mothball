using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Specifications;
using Moq;
using MothballMobile.UI.Features.Items.ItemsList;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Items.ItemsList;

[TestFixture]
public sealed class ItemsListViewModelTests
{
    [Test]
    public async Task InitializeAsync_LoadsFirstPage()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var snapshots = new List<InventorySnapshot> { new(item, 1, 0, []) };
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10)).ReturnsAsync(snapshots);
        var viewModel = CreateViewModel(queries.Object);

        await viewModel.InitializeAsync();

        Assert.That(viewModel.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchCommand_WithQuery_ReplacesListWithFilteredResults()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, "Widget", null, null))
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        viewModel.Query = "Widget";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.That(viewModel.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RefreshCommand_ReloadsFirstPage()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IItemsListQueryHandler>();
        queries.SetupSequence(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10))
            .ReturnsAsync([])
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.That(viewModel.Items, Has.Count.EqualTo(1));
    }

    private static ItemsListViewModel CreateViewModel(IItemsListQueryHandler queries)
    {
        var paths = new Mock<IImagePathResolver>();
        paths.Setup(p => p.GetItemPhotoPaths(It.IsAny<Item>())).Returns(Array.Empty<string>());

        return new ItemsListViewModel(
            paths.Object,
            queries,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(),
            Mock.Of<IBackgroundTaskObserver>());
    }
}
