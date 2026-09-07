using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Tags;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.UI.Features.Tags.TagsList;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Tags.TagsList;

[TestFixture]
public sealed class TagsListViewModelTests
{
    [Test]
    public async Task InitializeAsync_RefreshesCountsWhenReturningToThePage()
    {
        var tagId = Guid.NewGuid();
        var repository = new Mock<ITagRepository>();
        repository.SetupSequence(tags => tags.GetUsageSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TagUsageSummary(tagId, "winter", 1, 0)])
            .ReturnsAsync([new TagUsageSummary(tagId, "winter", 2, 1)]);
        var viewModel = new TagsListViewModel(repository.Object, Mock.Of<INavigationService>());

        await viewModel.InitializeAsync();
        await viewModel.InitializeAsync();

        Assert.That(viewModel.Tags.Single().Summary, Is.EqualTo(new TagUsageSummary(tagId, "winter", 2, 1)));
        repository.Verify(tags => tags.GetUsageSummariesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
