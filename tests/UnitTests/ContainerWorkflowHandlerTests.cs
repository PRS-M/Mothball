using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Specifications;
using Moq;

namespace UnitTests;

[TestFixture]
public sealed class ContainerWorkflowHandlerTests
{
    [Test]
    public async Task ContainerDetailsQueryHandler_WhenContainerExists_ReturnsContainerWithTotalItemCount()
    {
        var containerId = Guid.NewGuid();
        var container = new Container(containerId, "Box", "Notes");
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetContainerAsync(containerId.ToString()))
            .ReturnsAsync(container);
        queries.Setup(q => q.GetItemCountInContainerAsync(containerId.ToString()))
            .ReturnsAsync(4);

        var handler = new ContainerDetailsQueryHandler(queries.Object);

        var result = await handler.GetDetailsAsync(containerId.ToString());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Container, Is.SameAs(container));
            Assert.That(result.TotalItemCount, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task ContainerDetailsQueryHandler_WhenContainerMissing_DoesNotQueryItemCount()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.GetContainerAsync("missing"))
            .ReturnsAsync((Container?)null);

        var handler = new ContainerDetailsQueryHandler(queries.Object);

        var result = await handler.GetDetailsAsync("missing");

        Assert.That(result, Is.Null);
        queries.Verify(q => q.GetItemCountInContainerAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ContainerAssociationQueryHandler_QueryUnassignedItems_UsesUnassignedFilter()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var item = new Item("Hammer", string.Empty, totalQuantity: 3);
        var expected = new List<CoreApp.Contracts.ItemInventorySummary>
        {
            new(item, 1, [new CoreApp.Contracts.ItemContainerAllocation(Guid.NewGuid(), "Box", 1)]),
        };
        ItemListSpecification? capturedSpecification = null;

        queries.Setup(q => q.QueryItemInventorySummariesAsync(It.IsAny<ItemListSpecification>()))
            .Callback<ItemListSpecification>(s => capturedSpecification = s)
            .ReturnsAsync(expected);

        var handler = new ContainerAssociationQueryHandler(queries.Object);

        var excludedContainerId = Guid.NewGuid();
        var result = await handler.QueryUnassignedItemsAsync(
            pageNumber: 2,
            pageSize: 10,
            excludedContainerId);

        Assert.That(result, Is.SameAs(expected));
        Assert.That(capturedSpecification, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedSpecification!.Filter, Is.EqualTo(ItemQueryFilter.Unassigned));
            Assert.That(capturedSpecification.PageNumber, Is.EqualTo(2));
            Assert.That(capturedSpecification.PageSize, Is.EqualTo(10));
            Assert.That(capturedSpecification.ExcludedContainerId, Is.EqualTo(excludedContainerId));
        });
    }

    [Test]
    public async Task AssignItemToContainerCommandHandler_AssignsDefaultQuantity()
    {
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();
        var commands = new Mock<IItemInventoryCommandService>();
        var handler = new AssignItemToContainerCommandHandler(commands.Object);

        await handler.AssignAsync(itemId, containerId);

        commands.Verify(c => c.SetContainerAllocationAsync(itemId, containerId, 1), Times.Once);
    }
}
