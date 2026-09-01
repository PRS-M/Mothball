using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Entities.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using MothballMobile.UI.Features.Items.Consumption;
using MothballMobile.UI.Features.Items.Quantity;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Presentation.Popups;
using MothballMobile.UI.Features.Items.ItemDetails;
using MothballMobile.UI.Features.Items.ItemLocations;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Items;

[TestFixture]
public sealed class ItemDetailsViewModelTests
{
    [Test]
    public void DisplayDescription_UsesPlaceholderForEmptyDescription()
    {
        var viewModel = CreateViewModel(
            Mock.Of<IItemDetailsQueryHandler>(),
            Mock.Of<IItemInventoryCommandService>(),
            Mock.Of<IPopupService>());

        Assert.That(viewModel.DisplayDescription, Is.EqualTo("No description."));

        viewModel.Description = "Stored in the workshop.";

        Assert.That(viewModel.DisplayDescription, Is.EqualTo("Stored in the workshop."));
    }

    [Test]
    public async Task DeletePhotoCommand_WhenConfirmed_DeletesPhotoAndRefreshesPaths()
    {
        var image = new ImageItem(Guid.NewGuid());
        var item = new Item(Guid.NewGuid(), "Widget", "");
        item.AddImageItems([image]);
        var details = new ItemDetailsResult(new InventorySnapshot(item, 1, 0, []));
        var itemDetails = CreateItemDetailsQuery(item.ItemId, details);

        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.SelectOptionAsync(It.IsAny<OptionPickerPopupDefinition<ImageItem>>()))
            .ReturnsAsync(image);
        popup.Setup(p => p.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>()))
            .ReturnsAsync(true);

        var photoDeletion = new Mock<IPhotoDeletionService>();
        photoDeletion.Setup(d => d.DeleteItemPhotoAsync(item, image.ImageId)).ReturnsAsync(true);
        var imageService = new ImageService(
            Mock.Of<IPhotoSourceReader>(),
            Mock.Of<IPhotoFilePersistenceService>(),
            Mock.Of<ITemporaryPhotoService>(),
            photoDeletion.Object,
            Mock.Of<IInventoryCommandRepository>());

        var paths = new Mock<IImagePathResolver>();
        paths.Setup(p => p.GetItemPhotoPaths(item)).Returns(["updated.png"]);
        paths.Setup(p => p.GetFallbackImagePath()).Returns("fallback.png");

        var viewModel = new ItemDetailsViewModel(
            CreateCoordinator(itemDetails.Object, Mock.Of<IItemInventoryCommandService>(), popup.Object),
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(),
            paths.Object,
            popup.Object,
            new PopupDefinitionService(),
            imageService,
            Mock.Of<IPhotoBackgroundOperationTracker>(),
            Mock.Of<IBackgroundTaskObserver>());
        await viewModel.InitializeAsync(item.ItemId.ToString());

        await viewModel.DeletePhotoCommand.ExecuteAsync(null);

        photoDeletion.Verify(d => d.DeleteItemPhotoAsync(item, image.ImageId), Times.Once);
        Assert.That(viewModel.ImagePaths.Single(), Is.EqualTo("updated.png"));
    }

    [Test]
    public async Task InitializeAsync_WhenItemIsUnassignedOnly_ShowsAssociateAndHidesGoToContainer()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var details = new ItemDetailsResult(new InventorySnapshot(item, 4, 0, []));
        var itemDetails = CreateItemDetailsQuery(item.ItemId, details);

        var viewModel = CreateViewModel(itemDetails.Object, Mock.Of<IItemInventoryCommandService>(), Mock.Of<IPopupService>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParams.ItemId] = item.ItemId.ToString() });

        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasUnassignedQuantity, Is.True);
            Assert.That(viewModel.ShowGoToContainerButton, Is.False);
        });
    }

    [Test]
    public async Task InitializeAsync_WhenItemIsFullyAssignedToOneContainer_ShowsGoToContainerAndHidesAssociate()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 4);
        var details = new ItemDetailsResult(new InventorySnapshot(item, 4, 4, [allocation]));
        var itemDetails = CreateItemDetailsQuery(item.ItemId, details);

        var viewModel = CreateViewModel(itemDetails.Object, Mock.Of<IItemInventoryCommandService>(), Mock.Of<IPopupService>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParams.ItemId] = item.ItemId.ToString() });

        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasUnassignedQuantity, Is.False);
            Assert.That(viewModel.ShowGoToContainerButton, Is.True);
        });
    }

    [Test]
    public async Task InitializeAsync_WhenItemIsAssignedAndUnassigned_ShowsBothContainerActions()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 4);
        var details = new ItemDetailsResult(new InventorySnapshot(item, 6, 4, [allocation]));
        var itemDetails = CreateItemDetailsQuery(item.ItemId, details);

        var viewModel = CreateViewModel(itemDetails.Object, Mock.Of<IItemInventoryCommandService>(), Mock.Of<IPopupService>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParams.ItemId] = item.ItemId.ToString() });

        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasUnassignedQuantity, Is.True);
            Assert.That(viewModel.ShowGoToContainerButton, Is.True);
        });
    }

    [Test]
    public async Task NavigateToContainerCommand_WhenOneAllocation_NavigatesDirectlyToContainer()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var containerId = Guid.NewGuid();
        var allocation = new ItemContainerAllocation(containerId, "Box", 4);
        var details = new ItemDetailsResult(new InventorySnapshot(item, 4, 4, [allocation]));
        var itemDetails = CreateItemDetailsQuery(item.ItemId, details);
        var nav = new Mock<INavigationService>();

        var viewModel = CreateViewModel(
            itemDetails.Object,
            Mock.Of<IItemInventoryCommandService>(),
            Mock.Of<IPopupService>(),
            nav.Object);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParams.ItemId] = item.ItemId.ToString() });
        await viewModel.InitializeAsync();

        await viewModel.NavigateToContainerCommand.ExecuteAsync(null);

        nav.Verify(n => n.GoToAsync(
            NavigationRoutes.ContainerDetails,
            It.Is<ContainerDetailsNavigationRequest>(request => request.ContainerId == containerId)), Times.Once);
    }

    [Test]
    public async Task NavigateToContainerCommand_WhenMultipleAllocations_NavigatesToItemLocations()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var allocations = new[]
        {
            new ItemContainerAllocation(Guid.NewGuid(), "Box", 3),
            new ItemContainerAllocation(Guid.NewGuid(), "Drawer", 3),
        };
        var details = new ItemDetailsResult(new InventorySnapshot(item, 6, 6, allocations));
        var itemDetails = CreateItemDetailsQuery(item.ItemId, details);
        var nav = new Mock<INavigationService>();

        var viewModel = CreateViewModel(
            itemDetails.Object,
            Mock.Of<IItemInventoryCommandService>(),
            Mock.Of<IPopupService>(),
            nav.Object);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParams.ItemId] = item.ItemId.ToString() });
        await viewModel.InitializeAsync();

        await viewModel.NavigateToContainerCommand.ExecuteAsync(null);

        nav.Verify(n => n.GoToAsync(
            NavigationRoutes.ItemLocations,
            It.Is<ItemLocationsNavigationRequest>(request => request.ItemId == item.ItemId)), Times.Once);
    }

    [Test]
    public async Task ItemLocationsViewModel_UsesContainersForTilesAndLoadsImages()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var container = new CoreApp.Domain.Entities.ContainerAggregate.Container(Guid.NewGuid(), "Box", "Shelf");
        var allocation = new ItemContainerAllocation(container.ContainerId, container.Name, 3);
        var details = new ItemDetailsResult(new InventorySnapshot(item, 3, 3, [allocation]));
        var itemDetails = CreateItemDetailsQuery(item.ItemId, details);

        var inventoryQueries = new Mock<IInventoryQueryRepository>();
        inventoryQueries.Setup(q => q.GetContainerAsync(container.ContainerId.ToString()))
            .ReturnsAsync(container);

        var paths = new Mock<IImagePathResolver>();
        paths.Setup(p => p.GetContainerPhotoPaths(container))
            .Returns(["box.png"]);

        var viewModel = new ItemLocationsViewModel(
            itemDetails.Object,
            inventoryQueries.Object,
            paths.Object,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode));
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParams.ItemId] = item.ItemId.ToString() });

        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ItemName, Is.EqualTo("Widget"));
            Assert.That(viewModel.Locations, Has.Count.EqualTo(1));
            Assert.That(viewModel.Locations[0].Name, Is.EqualTo("Box"));
            Assert.That(viewModel.Locations[0].Notes, Is.EqualTo("Shelf"));
            Assert.That(viewModel.Locations[0].ItemCount, Is.EqualTo("Quantity here: 3"));
            Assert.That(viewModel.Locations[0].ImagePaths.Single(), Is.EqualTo("box.png"));
        });
    }

    [Test]
    public async Task EditQuantityCommand_WhenPickerReturnReappearsPageAndTotalWasReset_UsesPrePickerSnapshotForDecrease()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var sourceContainerId = Guid.NewGuid();
        var allocation = new ItemContainerAllocation(sourceContainerId, "Box", 7);
        var inventory = new InventorySnapshot(item, 10, 7, [allocation]);
        var details = new ItemDetailsResult(inventory);

        var itemDetails = new Mock<IItemDetailsQueryHandler>();
        itemDetails.Setup(q => q.GetDetailsAsync(item.ItemId.ToString()))
            .ReturnsAsync(details);

        var inventoryCommands = new Mock<IItemInventoryCommandService>(MockBehavior.Strict);
        inventoryCommands.Setup(c => c.ApplyWithdrawalAsync(
                item.ItemId,
                It.Is<ItemInventoryWithdrawalPlan>(plan =>
                    plan.TotalQuantity == 5
                    && plan.AssignedQuantity == 2
                    && plan.UnassignedQuantity == 3
                    && plan.Allocations.Single().ContainerId == sourceContainerId
                    && plan.Allocations.Single().Quantity == 2)))
            .ReturnsAsync(new ItemInventoryUpdateResult(
                RemovedFromContainer: false,
                TotalQuantity: 5,
                AssignedQuantity: 2,
                UnassignedQuantity: 3));

        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        ItemDetailsViewModel? viewModel = null;
        popup.Setup(p => p.PickNumberAsync(It.Is<NumberPickerPopupDefinition>(
                definition => definition.Title == "Set total quantity")))
            .Returns(() =>
            {
                viewModel!.TotalQuantity = 0;
                return Task.FromResult<int?>(5);
            });
        popup.Setup(p => p.PickNumberAsync(It.Is<NumberPickerPopupDefinition>(
                definition => definition.Title == "Withdraw from Box")))
            .ReturnsAsync(5);

        viewModel = CreateViewModel(
            itemDetails.Object,
            inventoryCommands.Object,
            popup.Object);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = item.ItemId.ToString(),
            [NavigationParams.ContainerId] = sourceContainerId.ToString(),
        });
        await viewModel.InitializeAsync();

        await viewModel.EditQuantityCommand.ExecuteAsync(null);

        inventoryCommands.Verify(c => c.ApplyWithdrawalAsync(item.ItemId, It.IsAny<ItemInventoryWithdrawalPlan>()), Times.Once);
        inventoryCommands.Verify(c => c.IncreaseTotalQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.TotalQuantity, Is.EqualTo(5));
            Assert.That(viewModel.AssignedQuantity, Is.EqualTo(2));
            Assert.That(viewModel.UnassignedQuantity, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task UseCommand_FromContainerContext_ConsumesAndRefreshesQuantities()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var containerId = Guid.NewGuid();
        var before = new InventorySnapshot(
            item,
            3,
            2,
            [new ItemContainerAllocation(containerId, "Box", 2)]);
        var after = new InventorySnapshot(
            item,
            2,
            1,
            [new ItemContainerAllocation(containerId, "Box", 1)]);
        var itemDetails = new Mock<IItemDetailsQueryHandler>();
        itemDetails.SetupSequence(q => q.GetDetailsAsync(item.ItemId.ToString()))
            .ReturnsAsync(new ItemDetailsResult(before))
            .ReturnsAsync(new ItemDetailsResult(before))
            .ReturnsAsync(new ItemDetailsResult(after));
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(c => c.ConsumeAsync(
                item.ItemId,
                ItemInventoryConsumptionSource.FromContainer(containerId),
                1))
            .ReturnsAsync(new ItemInventoryUpdateResult(false, 2, 1, 1));
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>())).ReturnsAsync(true);
        popup.Setup(p => p.PickNumberAsync(It.IsAny<NumberPickerPopupDefinition>())).ReturnsAsync(1);
        var viewModel = CreateViewModel(itemDetails.Object, commands.Object, popup.Object);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationParams.ItemId] = item.ItemId.ToString(),
            [NavigationParams.ContainerId] = containerId.ToString(),
        });
        await viewModel.InitializeAsync();

        await viewModel.UseCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.TotalQuantity, Is.EqualTo(2));
            Assert.That(viewModel.AssignedQuantity, Is.EqualTo(1));
            Assert.That(viewModel.UnassignedQuantity, Is.EqualTo(1));
        });
    }

    private static ItemDetailsViewModel CreateViewModel(
        IItemDetailsQueryHandler itemDetails,
        IItemInventoryCommandService inventoryCommands,
        IPopupService popup,
        INavigationService? nav = null)
        => new(
            CreateCoordinator(itemDetails, inventoryCommands, popup),
            nav ?? Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode),
            CreatePaths(),
            popup,
            new PopupDefinitionService(),
            CreateImageService(),
            Mock.Of<IPhotoBackgroundOperationTracker>(),
            Mock.Of<IBackgroundTaskObserver>());

    private static ItemDetailsCoordinator CreateCoordinator(
        IItemDetailsQueryHandler itemDetails,
        IItemInventoryCommandService inventoryCommands,
        IPopupService popup)
        => new(
            itemDetails,
            Mock.Of<IDeleteItemCommandHandler>(),
            Mock.Of<IUpdateItemDescriptionCommandHandler>(),
            new ItemConsumptionCoordinator(itemDetails, inventoryCommands, popup, new PopupDefinitionService()),
            new ItemQuantityEditCoordinator(
                itemDetails,
                inventoryCommands,
                new ItemInventoryWithdrawalCoordinator(inventoryCommands, popup, new PopupDefinitionService()),
                popup,
                new PopupDefinitionService()),
            NullLogger<ItemDetailsCoordinator>.Instance);

    private static Mock<IItemDetailsQueryHandler> CreateItemDetailsQuery(Guid itemId, ItemDetailsResult details)
    {
        var itemDetails = new Mock<IItemDetailsQueryHandler>();
        itemDetails.Setup(q => q.GetDetailsAsync(itemId.ToString()))
            .ReturnsAsync(details);
        return itemDetails;
    }

    private static IImagePathResolver CreatePaths()
    {
        var paths = new Mock<IImagePathResolver>();
        paths.Setup(p => p.GetItemPhotoPaths(It.IsAny<Item>()))
            .Returns(Array.Empty<string>());
        paths.Setup(p => p.GetFallbackImagePath())
            .Returns("fallback.png");
        return paths.Object;
    }

    private static ImageService CreateImageService()
        => new(
            Mock.Of<IPhotoSourceReader>(),
            Mock.Of<IPhotoFilePersistenceService>(),
            Mock.Of<ITemporaryPhotoService>(),
            Mock.Of<IPhotoDeletionService>(),
            Mock.Of<IInventoryCommandRepository>());
}
