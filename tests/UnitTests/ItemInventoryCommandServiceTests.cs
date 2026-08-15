using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using Moq;

namespace UnitTests;

[TestFixture]
public sealed class ItemInventoryCommandServiceTests
{
    [Test]
    public async Task SetTotalQuantityAsync_WhenValid_UpdatesItemAndReturnsSummary()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 5);
        item.SetAssignedQuantity(2);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.SetTotalQuantityAsync(item.ItemId, 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(7));
            Assert.That(result.AssignedQuantity, Is.EqualTo(2));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(5));
        });
        commands.Verify(c => c.UpdateItemAsync(item), Times.Once);
    }

    [Test]
    public void SetTotalQuantityAsync_BelowAssigned_RejectsWithoutPersisting()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 5);
        item.SetAssignedQuantity(3);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.SetTotalQuantityAsync(item.ItemId, 2));
        commands.Verify(c => c.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    [Test]
    public async Task SetContainerAllocationAsync_ReplacesAllocationUsingItsDelta()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 10);
        item.SetAssignedQuantity(7);
        var container = new Container(Guid.NewGuid(), "Box", "");
        container.AddItem(item.ItemId, 3);
        var queries = CreateQueries(item, container);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.AssignedQuantity, Is.EqualTo(8));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(2));
        });
        commands.Verify(c => c.SetItemContainerAllocationAsync(item, container.ContainerId, 4), Times.Once);
    }

    [Test]
    public async Task SetContainerAllocationAsync_WhenResultExceedsTotal_IncreasesTotalAndPersistsAllocation()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 7);
        item.SetAssignedQuantity(7);
        var container = new Container(Guid.NewGuid(), "Box", "");
        container.AddItem(item.ItemId, 3);
        var queries = CreateQueries(item, container);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(8));
            Assert.That(result.AssignedQuantity, Is.EqualTo(8));
            Assert.That(result.UnassignedQuantity, Is.Zero);
        });
        commands.Verify(c => c.SetItemContainerAllocationAsync(item, container.ContainerId, 4), Times.Once);
        commands.Verify(c => c.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    [Test]
    public async Task SetContainerAllocationAsync_WithZero_ReleasesQuantity()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 10);
        item.SetAssignedQuantity(7);
        var container = new Container(Guid.NewGuid(), "Box", "");
        container.AddItem(item.ItemId, 3);
        var queries = CreateQueries(item, container);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.RemovedFromContainer, Is.True);
            Assert.That(result.AssignedQuantity, Is.EqualTo(4));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(6));
        });
        commands.Verify(c => c.SetItemContainerAllocationAsync(item, container.ContainerId, 0), Times.Once);
        commands.Verify(c => c.DeleteItemContainerRelation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    private static Mock<IInventoryQueryRepository> CreateQueries(Item item, Container container)
    {
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        queries.Setup(q => q.GetContainerAsync(container.ContainerId.ToString())).ReturnsAsync(container);
        return queries;
    }
}