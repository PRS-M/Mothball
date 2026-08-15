using CoreApp.Contracts;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using MothballMobile.UI.Features.Items.ItemDetails;

namespace UnitTests;

[TestFixture]
public sealed class ItemDetailsViewModelTests
{
    [Test]
    public async Task SetTotalQuantityCommand_WhenPickerReturnReappearsPageAndTotalWasReset_UsesPrePickerSnapshotForDecrease()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 10);
        var sourceContainerId = Guid.NewGuid();
        var allocation = new ItemContainerAllocation(sourceContainerId, "Box", 7);
        var inventory = new ItemInventorySummary(item, assignedQuantity: 7, [allocation]);
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

        await viewModel.SetTotalQuantityCommand.ExecuteAsync(null);

        inventoryCommands.Verify(c => c.ApplyWithdrawalAsync(item.ItemId, It.IsAny<ItemInventoryWithdrawalPlan>()), Times.Once);
        inventoryCommands.Verify(c => c.IncreaseTotalQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.TotalQuantity, Is.EqualTo(5));
            Assert.That(viewModel.AssignedQuantity, Is.EqualTo(2));
            Assert.That(viewModel.UnassignedQuantity, Is.EqualTo(3));
        });
    }

    private static ItemDetailsViewModel CreateViewModel(
        IItemDetailsQueryHandler itemDetails,
        IItemInventoryCommandService inventoryCommands,
        IPopupService popup)
        => new(
            itemDetails,
            inventoryCommands,
            Mock.Of<IDeleteItemCommandHandler>(),
            Mock.Of<INavigationService>(),
            CreatePaths(),
            popup,
            new PopupDefinitionService(),
            CreateImageService(),
            Mock.Of<IPhotoBackgroundOperationTracker>(),
            Mock.Of<IBackgroundTaskObserver>(),
            NullLogger<ItemDetailsViewModel>.Instance);

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
