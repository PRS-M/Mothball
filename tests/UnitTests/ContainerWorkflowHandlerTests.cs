using CoreApp.Entities.Inventory;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Contracts;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Specifications;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

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
        var item = new Item("Hammer", string.Empty);
        var expected = new List<CoreApp.Entities.Inventory.InventorySnapshot>
        {
            new(item, 3, 1, [new CoreApp.Entities.Inventory.ItemContainerAllocation(Guid.NewGuid(), "Box", 1)]),
        };
        ItemListSpecification? capturedSpecification = null;

        queries.Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<ItemListSpecification>()))
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

    [Test]
    public async Task AssociateItemWithContainerViewModel_SelectContainer_AssignsSelectedUnassignedQuantity()
    {
        var itemId = Guid.NewGuid();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var imagePaths = new Mock<IImagePathResolver>();
        imagePaths.Setup(p => p.GetContainerPhotoPaths(container))
            .Returns(Array.Empty<string>());

        var associationQueries = new Mock<IContainerAssociationQueryHandler>();
        associationQueries.Setup(q => q.QueryContainersAsync(0, 10))
            .ReturnsAsync([container]);

        var item = new Item(itemId, "Widget", "");
        var itemDetails = new Mock<IItemDetailsQueryHandler>();
        itemDetails.Setup(q => q.GetDetailsAsync(itemId.ToString()))
            .ReturnsAsync(new ItemDetailsResult(new InventorySnapshot(item, 5, 0, [])));

        var assign = new Mock<IAssignItemToContainerCommandHandler>();
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.PickNumberAsync(It.Is<NumberPickerPopupDefinition>(
                definition => definition.Max == 5)))
            .ReturnsAsync(3);

        var nav = new Mock<INavigationService>();
        var viewModel = new AssociateItemWithContainerViewModel(
            imagePaths.Object,
            associationQueries.Object,
            itemDetails.Object,
            assign.Object,
            nav.Object,
            Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode == true),
            popup.Object,
            new PopupDefinitionService(),
            Mock.Of<IBackgroundTaskObserver>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = itemId.ToString(),
            [NavigationParams.UnassignedQuantity] = 5,
        });
        await viewModel.InitializeAsync();

        await viewModel.Containers.Single().SelectCommand.ExecuteAsync(null);

        assign.Verify(a => a.AssignAsync(itemId, container.ContainerId, 3), Times.Once);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Test]
    public async Task AssociateItemWithContainerViewModel_SelectAlreadyAssignedContainer_AllowsExistingPlusUnassignedQuantity()
    {
        var itemId = Guid.NewGuid();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var imagePaths = new Mock<IImagePathResolver>();
        imagePaths.Setup(p => p.GetContainerPhotoPaths(container))
            .Returns(Array.Empty<string>());

        var associationQueries = new Mock<IContainerAssociationQueryHandler>();
        associationQueries.Setup(q => q.QueryContainersAsync(0, 10))
            .ReturnsAsync([container]);

        var item = new Item(itemId, "Widget", "");
        var allocation = new ItemContainerAllocation(container.ContainerId, container.Name, 1);
        var itemDetails = new Mock<IItemDetailsQueryHandler>();
        itemDetails.Setup(q => q.GetDetailsAsync(itemId.ToString()))
            .ReturnsAsync(new ItemDetailsResult(new InventorySnapshot(item, 4, 1, [allocation])));

        var assign = new Mock<IAssignItemToContainerCommandHandler>();
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.PickNumberAsync(It.Is<NumberPickerPopupDefinition>(
                definition => definition.Max == 4 && definition.InitialValue == 4)))
            .ReturnsAsync(4);

        var nav = new Mock<INavigationService>();
        var viewModel = new AssociateItemWithContainerViewModel(
            imagePaths.Object,
            associationQueries.Object,
            itemDetails.Object,
            assign.Object,
            nav.Object,
            Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode == true),
            popup.Object,
            new PopupDefinitionService(),
            Mock.Of<IBackgroundTaskObserver>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = itemId.ToString(),
            [NavigationParams.UnassignedQuantity] = 3,
        });
        await viewModel.InitializeAsync();

        await viewModel.Containers.Single().SelectCommand.ExecuteAsync(null);

        assign.Verify(a => a.AssignAsync(itemId, container.ContainerId, 4), Times.Once);
    }

}
