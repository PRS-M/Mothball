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
    public async Task IncreaseTotalQuantityAsync_WhenValid_UpdatesItemAndReturnsSummary()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 5);
        item.SetAssignedQuantity(2);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.IncreaseTotalQuantityAsync(item.ItemId, 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(7));
            Assert.That(result.AssignedQuantity, Is.EqualTo(2));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(5));
        });
        commands.Verify(c => c.UpdateItemAsync(item), Times.Once);
    }

    [Test]
    public async Task IncreaseTotalQuantityAsync_WithStaleDecrease_ReturnsCurrentSnapshotWithoutPersisting()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 5);
        item.SetAssignedQuantity(3);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.IncreaseTotalQuantityAsync(item.ItemId, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(5));
            Assert.That(result.AssignedQuantity, Is.EqualTo(3));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(2));
        });
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

    [Test]
    public async Task ApplyWithdrawalAsync_WithRemainingStock_CommitsPlanAtomically()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 10);
        item.SetAssignedQuantity(7);
        var allocations = new[]
        {
            new CoreApp.Contracts.ItemContainerAllocation(Guid.NewGuid(), "Box", 2),
            new CoreApp.Contracts.ItemContainerAllocation(Guid.NewGuid(), "Drawer", 4),
        };
        var plan = new CoreApp.Contracts.ItemInventoryWithdrawalPlan(7, 6, 1, allocations, false);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.ApplyWithdrawalAsync(item.ItemId, plan);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(7));
            Assert.That(result.AssignedQuantity, Is.EqualTo(6));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(1));
        });
        commands.Verify(c => c.ApplyItemInventoryWithdrawalAsync(item, allocations), Times.Once);
    }

    [Test]
    public async Task ApplyWithdrawalAsync_SingleContainerCanReduceTotalBelowPreviousAssignedQuantity()
    {
        var containerId = Guid.NewGuid();
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 10);
        item.SetAssignedQuantity(10);
        var allocations = new[]
        {
            new CoreApp.Contracts.ItemContainerAllocation(containerId, "Box", 8),
        };
        var plan = new CoreApp.Contracts.ItemInventoryWithdrawalPlan(8, 8, 0, allocations, false);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.ApplyWithdrawalAsync(item.ItemId, plan);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(8));
            Assert.That(result.AssignedQuantity, Is.EqualTo(8));
            Assert.That(result.UnassignedQuantity, Is.Zero);
        });
        commands.Verify(c => c.ApplyItemInventoryWithdrawalAsync(item, allocations), Times.Once);
    }

    [Test]
    public async Task ApplyWithdrawalAsync_WhenPlanExhaustsStock_DeletesItemInsteadOfPersistingZero()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 1);
        var plan = new CoreApp.Contracts.ItemInventoryWithdrawalPlan(0, 0, 0, [], true);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        var commands = new Mock<IInventoryCommandRepository>();
        var photoDeletion = new Mock<IPhotoDeletionService>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object, photoDeletion.Object);

        var result = await service.ApplyWithdrawalAsync(item.ItemId, plan);

        Assert.That(result.ItemDeleted, Is.True);
        commands.Verify(c => c.DeleteItemAsync(item.ItemId.ToString()), Times.Once);
        photoDeletion.Verify(service => service.DeleteItemPhotoFilesBestEffortAsync(item), Times.Once);
        commands.Verify(c => c.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    private static Mock<IInventoryQueryRepository> CreateQueries(Item item, Container container)
    {
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetItemWithPhotosAsync(item.ItemId.ToString())).ReturnsAsync(item);
        queries.Setup(q => q.GetContainerAsync(container.ContainerId.ToString())).ReturnsAsync(container);
        return queries;
    }
}