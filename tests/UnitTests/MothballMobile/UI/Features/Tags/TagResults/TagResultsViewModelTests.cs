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
}
