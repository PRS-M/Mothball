using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Inventory;
using CoreApp.Application.Features.Inventory.Allocation;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Moq;

namespace Mothball.Tests.Unit.Core.Features.Inventory;

[TestFixture]
public sealed class ItemReceiptServiceTests
{
    [Test]
    public async Task ReceiveAsync_WithoutDestination_IncreasesTotalAndKeepsStockUnassigned()
    {
        var item = new Item(Guid.NewGuid(), "Tape", "");
        var queries = CreateQueries(item, totalQuantity: 3, allocations: []);
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(service => service.IncreaseTotalQuantityAsync(item.ItemId, 5))
            .ReturnsAsync(new ItemInventoryUpdateResult(false, 5, 0, 5));
        var service = new ItemReceiptService(queries.Object, commands.Object);

        var result = await service.ReceiveAsync(item.ItemId, 2);

        Assert.That(result, Is.EqualTo(new ItemInventoryUpdateResult(false, 5, 0, 5)));
        commands.Verify(service => service.SetContainerAllocationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task ReceiveAsync_WithDestination_IncreasesTotalAndDestinationAllocation()
    {
        var item = new Item(Guid.NewGuid(), "Tape", "");
        var containerId = Guid.NewGuid();
        var allocations = new[] { new ItemContainerAllocation(containerId, "Drawer", 3) };
        var queries = CreateQueries(item, totalQuantity: 5, allocations);
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(service => service.IncreaseTotalQuantityAsync(item.ItemId, 7))
            .ReturnsAsync(new ItemInventoryUpdateResult(false, 7, 3, 4));
        commands.Setup(service => service.SetContainerAllocationAsync(item.ItemId, containerId, 5))
            .ReturnsAsync(new ItemInventoryUpdateResult(false, 7, 5, 2));
        var service = new ItemReceiptService(queries.Object, commands.Object);

        var result = await service.ReceiveAsync(item.ItemId, 2, containerId);

        Assert.That(result, Is.EqualTo(new ItemInventoryUpdateResult(false, 7, 5, 2)));
        commands.Verify(service => service.IncreaseTotalQuantityAsync(item.ItemId, 7), Times.Once);
        commands.Verify(service => service.SetContainerAllocationAsync(item.ItemId, containerId, 5), Times.Once);
    }

    [Test]
    public void ReceiveAsync_WithNonPositiveQuantity_RejectsRequest()
    {
        var service = new ItemReceiptService(
            Mock.Of<IInventoryQueryRepository>(),
            Mock.Of<IItemInventoryCommandService>());

        var action = () => service.ReceiveAsync(Guid.NewGuid(), 0);

        Assert.That(action, Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    private static Mock<IInventoryQueryRepository> CreateQueries(
        Item item,
        int totalQuantity,
        IReadOnlyList<ItemContainerAllocation> allocations)
    {
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(repository => repository.GetInventorySnapshotAsync(item.ItemId))
            .ReturnsAsync(new InventorySnapshot(item, totalQuantity, allocations.Sum(allocation => allocation.Quantity), allocations));
        return queries;
    }
}