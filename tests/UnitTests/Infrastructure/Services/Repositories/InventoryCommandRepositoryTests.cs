using CoreApp.Domain.Entities.ItemAggregate;
using Infrastructure.Services.Repositories;
using Moq;

namespace Mothball.Tests.Unit.Infrastructure.Services.Repositories;

[TestFixture]
public sealed class InventoryCommandRepositoryTests
{
    [Test]
    public async Task InsertItemAsync_AfterSuccessfulWrite_AdvancesInventoryRevision()
    {
        var item = new Item(Guid.NewGuid(), "Item", string.Empty);
        var items = new Mock<IItemRepository>();
        var changes = new Mock<IInventoryChangeTracker>();
        var repository = CreateRepository(items.Object, changes.Object);

        await repository.InsertItemAsync(item);

        changes.Verify(tracker => tracker.MarkChanged(), Times.Once);
    }

    [Test]
    public void InsertItemAsync_WhenWriteFails_DoesNotAdvanceInventoryRevision()
    {
        var item = new Item(Guid.NewGuid(), "Item", string.Empty);
        var items = new Mock<IItemRepository>();
        items.Setup(repository => repository.InsertAsync(item))
            .ThrowsAsync(new InvalidOperationException("write failed"));
        var changes = new Mock<IInventoryChangeTracker>();
        var repository = CreateRepository(items.Object, changes.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.InsertItemAsync(item));

        changes.Verify(tracker => tracker.MarkChanged(), Times.Never);
    }

    private static InventoryCommandRepository CreateRepository(
        IItemRepository items,
        IInventoryChangeTracker changes)
        => new(
            Mock.Of<IContainerRepository>(),
            items,
            Mock.Of<IItemInventoryRepository>(),
            Mock.Of<IImageRepository>(),
            Mock.Of<IRelationRepository>(),
            changes);
}
