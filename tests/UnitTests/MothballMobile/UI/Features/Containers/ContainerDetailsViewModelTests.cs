using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Features.Containers.ContainerDetails;
using Moq;
using MothballMobile.UI.Features.Containers.ContainerDetails;
using MothballMobile.UI.Features.Items.Consumption;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Containers;

[TestFixture]
public sealed class ContainerDetailsViewModelTests
{
    [Test]
    public async Task InitializeAsync_PublishesHeaderAndPhotoBeforeItemRowsComplete()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Top shelf");
        container.AddImageItem();
        var summary = new ContainerDetailsSummary(container, ItemTypesCount: 3, TotalItemCount: 7);
        var details = new Mock<IContainerDetailsHandler>();
        details.Setup(handler => handler.GetSummaryAsync(container.ContainerId.ToString()))
            .ReturnsAsync(summary);
        var itemPage = new TaskCompletionSource<List<ContainerItemInventoryEntry>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = new Mock<IContainerDetailsQueryHandler>();
        queries.Setup(handler => handler.QueryItemsAsync(
                container.ContainerId.ToString(), null, 0, 5))
            .Returns(itemPage.Task);
        var paths = new Mock<IImagePathResolver>();
        paths.Setup(resolver => resolver.GetContainerPhotoPaths(container))
            .Returns(["container.jpg"]);
        var popup = Mock.Of<IPopupService>();
        var popupDefinitions = new PopupDefinitionService();
        var inventoryCommands = Mock.Of<IItemInventoryCommandService>();
        var itemCoordinator = new ContainerDetailsItemsCoordinator(
            details.Object,
            queries.Object,
            paths.Object,
            Mock.Of<INavigationService>(),
            popup,
            popupDefinitions,
            new ItemConsumptionCoordinator(
                Mock.Of<IItemDetailsQueryHandler>(),
                inventoryCommands,
                popup,
                popupDefinitions),
            Mock.Of<IBackgroundTaskObserver>());
        var viewModel = new ContainerDetailsViewModel(
            Mock.Of<IDeleteContainerCommandHandler>(),
            Mock.Of<IUpdateContainerNotesCommandHandler>(),
            paths.Object,
            popup,
            popupDefinitions,
            CreateImageService(),
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode),
            Mock.Of<IPhotoBackgroundOperationTracker>(),
            itemCoordinator,
            Mock.Of<IBackgroundTaskObserver>());

        var initialization = viewModel.InitializeAsync(container.ContainerId.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(initialization.IsCompleted, Is.False);
            Assert.That(viewModel.Name, Is.EqualTo("Garage"));
            Assert.That(viewModel.ContainerImagePaths, Is.EqualTo(new[] { "container.jpg" }));
            Assert.That(viewModel.Rows, Has.Count.EqualTo(1));
            Assert.That(viewModel.Rows.Single(), Is.SameAs(viewModel));
            Assert.That(viewModel.IsLoadingItems, Is.True);
        });

        itemPage.SetResult([]);
        await initialization;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsLoadingItems, Is.False);
            Assert.That(viewModel.IsItemListEmpty, Is.True);
        });
    }

    private static ImageService CreateImageService()
        => new(
            Mock.Of<IPhotoSourceReader>(),
            Mock.Of<IPhotoFilePersistenceService>(),
            Mock.Of<ITemporaryPhotoService>(),
            Mock.Of<IPhotoDeletionService>(),
            Mock.Of<IInventoryCommandRepository>());
}
