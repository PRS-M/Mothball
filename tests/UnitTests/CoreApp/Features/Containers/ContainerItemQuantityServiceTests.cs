using CoreApp.Domain.Entities.ContainerAggregate;
using Moq;

namespace Mothball.Tests.Unit.Core.Features.Containers;

[TestFixture]
public class ContainerItemQuantityServiceTests
{
    [Test]
    public async Task SaveQuantityAsync_WithPositiveQuantity_UpdatesInventoryWithoutMutatingContainerProjection()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var container = new Container(containerId, "Box", "Notes");
        container.AddItem(itemId, 1);
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(c => c.SetContainerAllocationAsync(itemId, containerId, 3))
            .ReturnsAsync(new CoreApp.Application.Contracts.Inventory.ItemInventoryUpdateResult(false, 3, 3, 0));
        var service = new ContainerItemQuantityService(commands.Object);

        var result = await service.SaveQuantityAsync(container, itemId, 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.RemovedFromContainer, Is.False);
            Assert.That(result.TotalQuantity, Is.EqualTo(3));
            Assert.That(result.AssignedQuantity, Is.EqualTo(3));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(0));
            Assert.That(container.Items.Single(i => i.ItemId == itemId).Quantity, Is.EqualTo(1));
        });

        commands.Verify(c => c.SetContainerAllocationAsync(itemId, containerId, 3), Times.Once);
    }

    [Test]
    public async Task SaveQuantityAsync_WithZeroQuantity_UpdatesInventoryWithoutMutatingContainerProjection()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var container = new Container(containerId, "Box", "Notes");
        container.AddItem(itemId, 2);
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(c => c.SetContainerAllocationAsync(itemId, containerId, 0))
            .ReturnsAsync(new CoreApp.Application.Contracts.Inventory.ItemInventoryUpdateResult(true, 2, 0, 2));
        var service = new ContainerItemQuantityService(commands.Object);

        var result = await service.SaveQuantityAsync(container, itemId, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.RemovedFromContainer, Is.True);
            Assert.That(result.TotalQuantity, Is.EqualTo(2));
            Assert.That(result.AssignedQuantity, Is.EqualTo(0));
            Assert.That(result.UnassignedQuantity, Is.EqualTo(2));
            Assert.That(container.Items.Single(i => i.ItemId == itemId).Quantity, Is.EqualTo(2));
        });

        commands.Verify(c => c.SetContainerAllocationAsync(itemId, containerId, 0), Times.Once);
    }
}
