using CoreApp.Application.Features.Containers.Queries;
using CoreApp.Application.Features.Items.Queries;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Specifications;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Navigation;
using MothballMobile.UI.Features.Tags.TagResults;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Tags.TagResults;

[TestFixture]
public sealed class TagResultsViewModelTests
{
    [Test]
    public async Task InitializeAsync_WhenRefreshIndicatorExecutesRefreshCommand_PreservesTheLoad()
    {
        var tagId = Guid.NewGuid();
        var item = new Item(Guid.NewGuid(), "Tagged item", "");
        var inventory = new InventorySnapshot(item, 1, 0, []);
        var container = new Container(Guid.NewGuid(), "Tagged container", "");
        var pendingItems = new TaskCompletionSource<List<InventorySnapshot>>();
        var itemQueries = new Mock<IItemsListQueryHandler>();
        var containerQueries = new Mock<IContainerListQueryHandler>();
        itemQueries.Setup(query => query.QueryAsync(
                ItemQueryFilter.All, null, null, null,
                It.IsAny<CoreApp.Application.Contracts.Tags.TagFilter?>()))
            .Returns(pendingItems.Task);
        containerQueries.Setup(query => query.QueryAsync(
                false, null, null, null,
                It.IsAny<CoreApp.Application.Contracts.Tags.TagFilter?>()))
            .ReturnsAsync([container]);
        using var viewModel = new TagResultsViewModel(
            itemQueries.Object, containerQueries.Object,
            Mock.Of<IImagePathResolver>(), Mock.Of<INavigationService>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.TagId] = tagId.ToString(),
            [NavigationParams.TagName] = "winter",
        });

        Task? refreshFeedback = null;
        viewModel.PropertyChanged += (_, args) =>
        {
            // MAUI RefreshView executes its command when a binding sets IsRefreshing to true.
            if (args.PropertyName == nameof(viewModel.IsRefreshing)
                && viewModel.IsRefreshing && refreshFeedback is null)
            {
                refreshFeedback = viewModel.RefreshCommand.ExecuteAsync(null);
            }
        };
        var initialization = viewModel.InitializeAsync();
        pendingItems.SetResult([inventory]);
        await initialization.WaitAsync(TimeSpan.FromSeconds(2));
        await refreshFeedback!.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Results.Select(result => result.TargetId),
                Is.EquivalentTo(new[] { item.ItemId, container.ContainerId }));
            Assert.That(viewModel.IsBusy, Is.False);
            Assert.That(viewModel.IsRefreshing, Is.False);
        });
        itemQueries.Verify(query => query.QueryAsync(
            ItemQueryFilter.All, null, null, null,
            It.IsAny<CoreApp.Application.Contracts.Tags.TagFilter?>()), Times.Once);
        containerQueries.Verify(query => query.QueryAsync(
            false, null, null, null,
            It.IsAny<CoreApp.Application.Contracts.Tags.TagFilter?>()), Times.Once);
    }

    [Test]
    public async Task ApplyQueryAttributes_AfterInitialAppearance_LoadsTheSelectedTag()
    {
        var item = new Item(Guid.NewGuid(), "Tagged item", "");
        var inventory = new InventorySnapshot(item, 1, 0, []);
        var tagId = Guid.NewGuid();
        var itemQueries = new Mock<IItemsListQueryHandler>();
        var containerQueries = new Mock<IContainerListQueryHandler>();
        itemQueries
            .Setup(query => query.QueryAsync(
                It.IsAny<ItemQueryFilter>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CoreApp.Application.Contracts.Tags.TagFilter?>()))
            .ReturnsAsync([]);
        itemQueries
            .Setup(query => query.QueryAsync(
                ItemQueryFilter.All,
                null,
                null,
                null,
                It.Is<CoreApp.Application.Contracts.Tags.TagFilter>(filter => filter.TagId == tagId
                    && filter.Names.SequenceEqual(new[] { "winter" }))))
            .ReturnsAsync([inventory]);
        containerQueries
            .Setup(query => query.QueryAsync(
                false,
                null,
                null,
                null,
                It.IsAny<CoreApp.Application.Contracts.Tags.TagFilter?>()))
            .ReturnsAsync([]);

        var viewModel = new TagResultsViewModel(
            itemQueries.Object,
            containerQueries.Object,
            Mock.Of<IImagePathResolver>(),
            Mock.Of<INavigationService>());

        await viewModel.InitializeAsync();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.TagId] = tagId.ToString(),
            [NavigationParams.TagName] = "winter",
        });

        Assert.That(SpinWait.SpinUntil(() => viewModel.Results.Count == 1, TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(viewModel.Results[0].Name, Is.EqualTo("Tagged item"));
    }

    [Test]
    public async Task RefreshWhileInitialLoadIsBusy_DoesNotReenterTheLoad()
    {
        var itemQueries = new Mock<IItemsListQueryHandler>(MockBehavior.Strict);
        var containerQueries = new Mock<IContainerListQueryHandler>(MockBehavior.Strict);
        var viewModel = new TagResultsViewModel(
            itemQueries.Object,
            containerQueries.Object,
            Mock.Of<IImagePathResolver>(),
            Mock.Of<INavigationService>());

        viewModel.IsBusy = true;

        await viewModel.RefreshCommand.ExecuteAsync(null);

        itemQueries.VerifyNoOtherCalls();
        containerQueries.VerifyNoOtherCalls();
    }
}
