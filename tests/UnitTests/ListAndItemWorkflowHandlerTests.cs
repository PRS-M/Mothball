using CoreApp.Entities.Inventory;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;
using Moq;

namespace UnitTests;

[TestFixture]
public sealed class ListAndItemWorkflowHandlerTests
{
    [Test]
    public async Task ContainerListQueryHandler_WhenEmptyOnly_UsesEmptyFilterAndPaging()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        ContainerListSpecification? captured = null;
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .Callback<ContainerListSpecification>(s => captured = s)
            .ReturnsAsync([]);

        var handler = new ContainerListQueryHandler(queries.Object);

        await handler.QueryAsync(emptyOnly: true, searchTerm: "box", pageNumber: 2, pageSize: 10);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.Filter, Is.EqualTo(ContainerQueryFilter.Empty));
            Assert.That(captured.SearchTerm, Is.EqualTo("box"));
            Assert.That(captured.PageNumber, Is.EqualTo(2));
            Assert.That(captured.PageSize, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task ItemsListQueryHandler_WhenUnassignedOnly_UsesUnassignedFilterAndPaging()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        ItemListSpecification? captured = null;
        queries.Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<ItemListSpecification>()))
            .Callback<ItemListSpecification>(s => captured = s)
            .ReturnsAsync([]);

        var handler = new ItemsListQueryHandler(queries.Object);

        await handler.QueryAsync(ItemQueryFilter.Unassigned, searchTerm: "hat", pageNumber: 1, pageSize: 20);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.Filter, Is.EqualTo(ItemQueryFilter.Unassigned));
            Assert.That(captured.SearchTerm, Is.EqualTo("hat"));
            Assert.That(captured.PageNumber, Is.EqualTo(1));
            Assert.That(captured.PageSize, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task ItemsListQueryHandler_WhenAssigned_UsesAssignedFilterAndPaging()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        ItemListSpecification? captured = null;
        queries.Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<ItemListSpecification>()))
            .Callback<ItemListSpecification>(s => captured = s)
            .ReturnsAsync([]);

        var handler = new ItemsListQueryHandler(queries.Object);

        await handler.QueryAsync(ItemQueryFilter.Assigned, searchTerm: "hat", pageNumber: 1, pageSize: 20);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.Filter, Is.EqualTo(ItemQueryFilter.Assigned));
            Assert.That(captured.SearchTerm, Is.EqualTo("hat"));
            Assert.That(captured.PageNumber, Is.EqualTo(1));
            Assert.That(captured.PageSize, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task ItemDetailsQueryHandler_WhenItemExists_ReturnsItemAndRelatedContainerId()
    {
        var item = new Item(Guid.NewGuid(), "Hat", "Blue");
        var containerId = Guid.NewGuid();
        var summary = new CoreApp.Entities.Inventory.InventorySnapshot(
            item,
            3,
            2,
            [new CoreApp.Entities.Inventory.ItemContainerAllocation(containerId, "Box", 2)]);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(summary);

        var handler = new ItemDetailsQueryHandler(queries.Object);

        var result = await handler.GetDetailsAsync(item.ItemId.ToString());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Inventory.Item, Is.SameAs(item));
            Assert.That(result.Inventory.AssignedQuantity, Is.EqualTo(2));
            Assert.That(result.Inventory.Allocations.Single().ContainerId, Is.EqualTo(containerId));
        });
    }

    [Test]
    public async Task CreateItemCommandHandler_CreatesItemWithContainerRelation()
    {
        var commands = new Mock<IInventoryCommandRepository>();
        var imageService = new ImageService(
            Mock.Of<IPhotoSourceReader>(),
            Mock.Of<IPhotoFilePersistenceService>(),
            Mock.Of<ITemporaryPhotoService>(),
            Mock.Of<IPhotoDeletionService>(),
            commands.Object);
        var containerId = Guid.NewGuid();
        var handler = new CreateItemCommandHandler(commands.Object, imageService);

        var item = await handler.CreateAsync("Hat", "Blue", containerId, quantity: 3);

        commands.Verify(c => c.InsertItemAsync(item), Times.Once);
        commands.Verify(c => c.InsertItemInventoryAsync(It.Is<ItemInventory>(inventory =>
            inventory.ItemId == item.ItemId
            && inventory.TotalQuantity == 3
            && inventory.Allocations.Single().ContainerId == containerId
            && inventory.Allocations.Single().Quantity == 3)), Times.Once);
    }
}
