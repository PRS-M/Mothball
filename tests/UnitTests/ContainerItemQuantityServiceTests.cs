using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using Moq;

namespace UnitTests;

[TestFixture]
public class ContainerItemQuantityServiceTests
{
    [Test]
    public async Task SaveQuantityAsync_WithPositiveQuantity_ReplacesRelationAndUpdatesAggregate()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var container = new Container(containerId, "Box", "Notes");
        container.AddItem(itemId, 1);
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(c => c.SetContainerAllocationAsync(itemId, containerId, 3))
            .ReturnsAsync(new CoreApp.Contracts.ItemInventoryUpdateResult(false, 3, 3, 0));
        var service = new ContainerItemQuantityService(commands.Object);

        var result = await service.SaveQuantityAsync(container, itemId, 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Removed, Is.False);
            Assert.That(result.TotalItemCount, Is.EqualTo(3));
            Assert.That(container.Items.Single(i => i.ItemId == itemId).Quantity, Is.EqualTo(3));
        });

        commands.Verify(c => c.SetContainerAllocationAsync(itemId, containerId, 3), Times.Once);
    }

    [Test]
    public async Task SaveQuantityAsync_WithZeroQuantity_DeletesRelationAndUpdatesAggregate()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var container = new Container(containerId, "Box", "Notes");
        container.AddItem(itemId, 2);
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(c => c.SetContainerAllocationAsync(itemId, containerId, 0))
            .ReturnsAsync(new CoreApp.Contracts.ItemInventoryUpdateResult(true, 2, 0, 2));
        var service = new ContainerItemQuantityService(commands.Object);

        var result = await service.SaveQuantityAsync(container, itemId, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Removed, Is.True);
            Assert.That(result.TotalItemCount, Is.EqualTo(0));
            Assert.That(container.Items, Is.Empty);
        });

        commands.Verify(c => c.SetContainerAllocationAsync(itemId, containerId, 0), Times.Once);
    }
}
