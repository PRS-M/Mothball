using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Moq;

namespace Mothball.Tests.Unit.Core.Features.Inventory;

[TestFixture]
public sealed class ItemInventoryCommandServiceTests
{
    [Test]
    public async Task IncreaseTotalQuantityAsync_WhenValid_UpdatesItemAndReturnsSummary()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(Summary(item, 2));
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.IncreaseTotalQuantityAsync(item.ItemId, 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(7));
            Assert.That(result.AssignedQuantity, Is.EqualTo(2));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(5));
        });
        commands.Verify(c => c.SaveItemInventoryAsync(It.Is<ItemInventory>(inventory =>
            inventory.ItemId == item.ItemId && inventory.TotalQuantity == 7)), Times.Once);
    }

    [Test]
    public async Task IncreaseTotalQuantityAsync_WithStaleDecrease_ReturnsCurrentSnapshotWithoutPersisting()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(Summary(item, 3));
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.IncreaseTotalQuantityAsync(item.ItemId, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(5));
            Assert.That(result.AssignedQuantity, Is.EqualTo(3));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(2));
        });
        commands.Verify(c => c.SaveItemInventoryAsync(It.IsAny<ItemInventory>()), Times.Never);
    }

    [Test]
    public async Task SetContainerAllocationAsync_ReplacesAllocationUsingItsDelta()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var container = new Container(Guid.NewGuid(), "Box", "");
        var queries = CreateQueries(item, container, containerQuantity: 3, totalQuantity: 10, assignedQuantity: 7);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.AssignedQuantity, Is.EqualTo(8));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(2));
        });
        commands.Verify(c => c.SaveItemInventoryAsync(It.Is<ItemInventory>(inventory =>
            inventory.ItemId == item.ItemId
            && inventory.TotalQuantity == 10
            && inventory.AssignedQuantity == 8
            && inventory.Allocations.Any(allocation => allocation.ContainerId == container.ContainerId && allocation.Quantity == 4))), Times.Once);
    }

    [Test]
    public async Task SetContainerAllocationAsync_WhenResultExceedsTotal_IncreasesTotalAndPersistsAllocation()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var container = new Container(Guid.NewGuid(), "Box", "");
        var queries = CreateQueries(item, container, containerQuantity: 3, totalQuantity: 7, assignedQuantity: 7);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(8));
            Assert.That(result.AssignedQuantity, Is.EqualTo(8));
            Assert.That(result.UnassignedQuantity, Is.Zero);
        });
        commands.Verify(c => c.SaveItemInventoryAsync(It.Is<ItemInventory>(inventory =>
            inventory.ItemId == item.ItemId
            && inventory.TotalQuantity == 8
            && inventory.AssignedQuantity == 8
            && inventory.Allocations.Any(allocation => allocation.ContainerId == container.ContainerId && allocation.Quantity == 4))), Times.Once);
        commands.Verify(c => c.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    [Test]
    public async Task SetContainerAllocationAsync_WithZero_ReleasesQuantity()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var container = new Container(Guid.NewGuid(), "Box", "");
        var queries = CreateQueries(item, container, containerQuantity: 3, totalQuantity: 10, assignedQuantity: 7);
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.RemovedFromContainer, Is.True);
            Assert.That(result.AssignedQuantity, Is.EqualTo(4));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(6));
        });
        commands.Verify(c => c.SaveItemInventoryAsync(It.Is<ItemInventory>(inventory =>
            inventory.ItemId == item.ItemId
            && inventory.TotalQuantity == 10
            && inventory.AssignedQuantity == 4
            && inventory.Allocations.All(allocation => allocation.ContainerId != container.ContainerId))), Times.Once);
        commands.Verify(c => c.DeleteItemContainerRelation(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task ApplyWithdrawalAsync_WithRemainingStock_CommitsPlanAtomically()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var allocations = new[]
        {
            new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(Guid.NewGuid(), "Box", 2),
            new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(Guid.NewGuid(), "Drawer", 4),
        };
        var plan = new CoreApp.Domain.Entities.InventoryAggregate.ItemInventoryWithdrawalPlan(7, 6, 1, allocations, false);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(Summary(item, 7));
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.ApplyWithdrawalAsync(item.ItemId, plan);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(7));
            Assert.That(result.AssignedQuantity, Is.EqualTo(6));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(1));
        });
        commands.Verify(c => c.SaveItemInventoryAsync(It.Is<ItemInventory>(inventory =>
            inventory.ItemId == item.ItemId
            && inventory.TotalQuantity == 7
            && inventory.AssignedQuantity == 6)), Times.Once);
    }

    [Test]
    public async Task ApplyWithdrawalAsync_SingleContainerCanReduceTotalBelowPreviousAssignedQuantity()
    {
        var containerId = Guid.NewGuid();
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var allocations = new[]
        {
            new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(containerId, "Box", 8),
        };
        var plan = new CoreApp.Domain.Entities.InventoryAggregate.ItemInventoryWithdrawalPlan(8, 8, 0, allocations, false);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(Summary(item, 10));
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object);

        var result = await service.ApplyWithdrawalAsync(item.ItemId, plan);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalQuantity, Is.EqualTo(8));
            Assert.That(result.AssignedQuantity, Is.EqualTo(8));
            Assert.That(result.UnassignedQuantity, Is.Zero);
        });
        commands.Verify(c => c.SaveItemInventoryAsync(It.Is<ItemInventory>(inventory =>
            inventory.ItemId == item.ItemId
            && inventory.TotalQuantity == 8
            && inventory.AssignedQuantity == 8)), Times.Once);
    }

    [Test]
    public async Task ApplyWithdrawalAsync_WhenPlanExhaustsStock_DeletesItemInsteadOfPersistingZero()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var plan = new CoreApp.Domain.Entities.InventoryAggregate.ItemInventoryWithdrawalPlan(0, 0, 0, [], true);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(Summary(item, 0));
        var commands = new Mock<IInventoryCommandRepository>();
        var photoDeletion = new Mock<IPhotoDeletionService>();
        var service = new ItemInventoryCommandService(queries.Object, commands.Object, photoDeletion.Object);

        var result = await service.ApplyWithdrawalAsync(item.ItemId, plan);

        Assert.That(result.ItemDeleted, Is.True);
        commands.Verify(c => c.DeleteItemAsync(item.ItemId.ToString()), Times.Once);
        photoDeletion.Verify(service => service.DeleteItemPhotoFilesBestEffortAsync(item), Times.Once);
        commands.Verify(c => c.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    private static Mock<IInventoryQueryRepository> CreateQueries(
        Item item,
        Container container,
        int containerQuantity,
        int totalQuantity,
        int assignedQuantity)
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var allocation = new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(
            container.ContainerId,
            container.Name,
            containerQuantity);
        var allocations = new List<CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation> { allocation };
        int remainingAssigned = assignedQuantity - allocation.Quantity;
        if (remainingAssigned > 0)
        {
            allocations.Add(new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(
                Guid.NewGuid(),
                "Other",
                remainingAssigned));
        }

        queries.Setup(q => q.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(new CoreApp.Domain.Entities.InventoryAggregate.InventorySnapshot(item, totalQuantity, assignedQuantity, allocations));
        return queries;
    }

    private static CoreApp.Domain.Entities.InventoryAggregate.InventorySnapshot Summary(Item item, int assignedQuantity)
        => new(
            item,
            Math.Max(5, assignedQuantity),
            assignedQuantity,
            assignedQuantity == 0
                ? []
                : [new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(Guid.NewGuid(), "Container", assignedQuantity)]);
}
