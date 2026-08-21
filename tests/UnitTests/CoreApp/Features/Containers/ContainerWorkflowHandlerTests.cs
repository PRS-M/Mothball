using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Contracts;
using CoreApp.Application.Specifications;
using CoreApp.Application.Features.Containers.ContainerDetails;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Presentation.Popups;
using MothballMobile.UI.Features.Containers.AssociateItemWithContainer;

namespace Mothball.Tests.Unit.Core.Features.Containers;

[TestFixture]
public sealed class ContainerWorkflowHandlerTests
{
    [Test]
    public async Task ContainerDetailsHandler_GetSummary_ReturnsContainerAndBothItemCounts()
    {
        var container = new Container(Guid.NewGuid(), "Box", "Notes");
        var queries = new Mock<IContainerDetailsQueryHandler>();
        queries.Setup(query => query.GetDetailsAsync(container.ContainerId.ToString()))
            .ReturnsAsync(new ContainerDetailsResult(container, 6));
        queries.Setup(query => query.GetDistinctItemCountAsync(container.ContainerId.ToString()))
            .ReturnsAsync(2);

        var handler = new ContainerDetailsHandler(queries.Object, Mock.Of<IContainerItemQuantityService>());

        var result = await handler.GetSummaryAsync(container.ContainerId.ToString());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Container, Is.SameAs(container));
            Assert.That(result.ItemTypesCount, Is.EqualTo(2));
            Assert.That(result.TotalItemCount, Is.EqualTo(6));
        });
    }

    [Test]
    public async Task ContainerDetailsHandler_SaveItemQuantity_ReturnsUpdatedCountsAndRemovalState()
    {
        var container = new Container(Guid.NewGuid(), "Box", "Notes");
        var itemId = Guid.NewGuid();
        var queries = new Mock<IContainerDetailsQueryHandler>();
        queries.Setup(query => query.GetDistinctItemCountAsync(container.ContainerId.ToString()))
            .ReturnsAsync(1);
        var quantities = new Mock<IContainerItemQuantityService>();
        quantities.Setup(service => service.SaveQuantityAsync(container, itemId, 0))
            .ReturnsAsync(new ContainerItemQuantityUpdateResult(
                TotalItemCount: 3,
                Inventory: new ItemInventoryUpdateResult(RemovedFromContainer: true, TotalQuantity: 0, AssignedQuantity: 0, UnassignedQuantity: 0)));

        var handler = new ContainerDetailsHandler(queries.Object, quantities.Object);

        var result = await handler.SaveItemQuantityAsync(container, itemId, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Inventory.RemovedFromContainer, Is.True);
            Assert.That(result.Summary.ItemTypesCount, Is.EqualTo(1));
            Assert.That(result.Summary.TotalItemCount, Is.EqualTo(3));
            Assert.That(result.Inventory.TotalQuantity, Is.EqualTo(0));
            Assert.That(result.Inventory.AssignedQuantity, Is.EqualTo(0));
            Assert.That(result.Inventory.UnassignedQuantity, Is.EqualTo(0));
        });
    }

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
        var expected = new List<CoreApp.Domain.Entities.InventoryAggregate.InventorySnapshot>
        {
            new(item, 3, 1, [new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(Guid.NewGuid(), "Box", 1)]),
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
    public async Task ContainerAssociationQueryHandler_QueryContainersWithSearch_UsesSearchTerm()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        ContainerListSpecification? capturedSpecification = null;
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .Callback<ContainerListSpecification>(specification => capturedSpecification = specification)
            .ReturnsAsync([]);
        var handler = new ContainerAssociationQueryHandler(queries.Object);

        await handler.QueryContainersAsync("archive");

        Assert.That(capturedSpecification, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedSpecification!.Filter, Is.EqualTo(ContainerQueryFilter.All));
            Assert.That(capturedSpecification.SearchTerm, Is.EqualTo("archive"));
            Assert.That(capturedSpecification.PageNumber, Is.Null);
            Assert.That(capturedSpecification.PageSize, Is.Null);
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
    public async Task ContainerItemAssociationHandler_GetAvailableQuantity_IncludesCurrentContainerAllocation()
    {
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();
        var item = new Item(itemId, "Widget", "");
        var details = new Mock<IItemDetailsQueryHandler>();
        details.Setup(query => query.GetDetailsAsync(itemId.ToString()))
            .ReturnsAsync(new ItemDetailsResult(new InventorySnapshot(
                item,
                5,
                3,
                [
                    new ItemContainerAllocation(containerId, "Box", 2),
                    new ItemContainerAllocation(Guid.NewGuid(), "Shelf", 1),
                ])));
        var handler = new ContainerItemAssociationHandler(
            details.Object,
            Mock.Of<IAssignItemToContainerCommandHandler>());

        var availableQuantity = await handler.GetAvailableQuantityAsync(itemId, containerId, 1);

        Assert.That(availableQuantity, Is.EqualTo(4));
    }

    [Test]
    public async Task ContainerItemAssociationHandler_TryAssociate_RejectsQuantityAboveAvailable()
    {
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();
        var item = new Item(itemId, "Widget", "");
        var details = new Mock<IItemDetailsQueryHandler>();
        details.Setup(query => query.GetDetailsAsync(itemId.ToString()))
            .ReturnsAsync(new ItemDetailsResult(new InventorySnapshot(item, 2, 0, [])));
        var assign = new Mock<IAssignItemToContainerCommandHandler>();
        var handler = new ContainerItemAssociationHandler(details.Object, assign.Object);

        var result = await handler.TryAssociateAsync(itemId, containerId, quantity: 3, fallbackUnassignedQuantity: 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Associated, Is.False);
            Assert.That(result.AvailableQuantity, Is.EqualTo(2));
        });
        assign.Verify(command => command.AssignAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
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

        var itemAssociation = new Mock<IContainerItemAssociationHandler>();
        itemAssociation.Setup(handler => handler.GetAvailableQuantityAsync(itemId, container.ContainerId, 5))
            .ReturnsAsync(5);
        itemAssociation.Setup(handler => handler.TryAssociateAsync(itemId, container.ContainerId, 3, 5))
            .ReturnsAsync(new ContainerItemAssociationResult(Associated: true, AvailableQuantity: 5));
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.PickNumberAsync(It.Is<NumberPickerPopupDefinition>(
                definition => definition.Max == 5)))
            .ReturnsAsync(3);

        var nav = new Mock<INavigationService>();
        var applicationSettings = Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode);
        var backgroundTasks = Mock.Of<IBackgroundTaskObserver>();
        var viewModel = new AssociateItemWithContainerViewModel(
            new AssociateItemWithContainerCoordinator(
                imagePaths.Object,
                associationQueries.Object,
                applicationSettings,
                backgroundTasks),
            itemAssociation.Object,
            nav.Object,
            applicationSettings,
            popup.Object,
            new PopupDefinitionService());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = itemId.ToString(),
            [NavigationParams.UnassignedQuantity] = 5,
        });
        await viewModel.InitializeAsync();

        await viewModel.Containers.Single().SelectCommand.ExecuteAsync(null);

        itemAssociation.Verify(handler => handler.TryAssociateAsync(itemId, container.ContainerId, 3, 5), Times.Once);
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

        var itemAssociation = new Mock<IContainerItemAssociationHandler>();
        itemAssociation.Setup(handler => handler.GetAvailableQuantityAsync(itemId, container.ContainerId, 3))
            .ReturnsAsync(4);
        itemAssociation.Setup(handler => handler.TryAssociateAsync(itemId, container.ContainerId, 4, 3))
            .ReturnsAsync(new ContainerItemAssociationResult(Associated: true, AvailableQuantity: 4));
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.PickNumberAsync(It.Is<NumberPickerPopupDefinition>(
                definition => definition.Max == 4 && definition.InitialValue == 4)))
            .ReturnsAsync(4);

        var nav = new Mock<INavigationService>();
        var applicationSettings = Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode);
        var backgroundTasks = Mock.Of<IBackgroundTaskObserver>();
        var viewModel = new AssociateItemWithContainerViewModel(
            new AssociateItemWithContainerCoordinator(
                imagePaths.Object,
                associationQueries.Object,
                applicationSettings,
                backgroundTasks),
            itemAssociation.Object,
            nav.Object,
            applicationSettings,
            popup.Object,
            new PopupDefinitionService());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = itemId.ToString(),
            [NavigationParams.UnassignedQuantity] = 3,
        });
        await viewModel.InitializeAsync();

        await viewModel.Containers.Single().SelectCommand.ExecuteAsync(null);

        itemAssociation.Verify(handler => handler.TryAssociateAsync(itemId, container.ContainerId, 4, 3), Times.Once);
    }

}
