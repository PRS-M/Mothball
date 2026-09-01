using CoreApp.Application.Specifications;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Infrastructure.Services.Repositories;
using Moq;

namespace Mothball.Tests.Unit.Infrastructure.Services.Repositories;

[TestFixture]
public sealed class InventoryQueryRepositoryTests
{
    [Test]
    public async Task QueryInventorySnapshotsAsync_LoadsPageInventoriesInOneBatch()
    {
        var first = new Item(Guid.NewGuid(), "First", string.Empty);
        var second = new Item(Guid.NewGuid(), "Second", string.Empty);
        var items = new Mock<IItemRepository>();
        items.Setup(repository => repository.QueryWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync([first, second]);
        var inventories = new Mock<IItemInventoryRepository>();
        inventories.Setup(repository => repository.GetManyAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ItemInventory>
            {
                [first.ItemId] = new(first.ItemId, 2, []),
                [second.ItemId] = new(second.ItemId, 3, []),
            });
        var repository = new InventoryQueryRepository(
            Mock.Of<IContainerRepository>(),
            items.Object,
            inventories.Object);

        var result = await repository.QueryInventorySnapshotsAsync(
            new ItemListSpecification(ItemQueryFilter.All, PageNumber: 0, PageSize: 10));

        Assert.That(result.Select(snapshot => snapshot.TotalQuantity), Is.EqualTo(new[] { 2, 3 }));
        inventories.Verify(
            inventoryRepository => inventoryRepository.GetManyAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids =>
                    ids.Count == 2 && ids.Contains(first.ItemId) && ids.Contains(second.ItemId))),
            Times.Once);
        inventories.Verify(
            inventoryRepository => inventoryRepository.GetAsync(It.IsAny<Guid>()),
            Times.Never);
    }
}
