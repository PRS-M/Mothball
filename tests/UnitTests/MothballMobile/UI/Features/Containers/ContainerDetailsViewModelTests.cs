using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Barcodes.Commands;
using Moq;
using MothballMobile.Infrastructure.Scanning;
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
        container.UpdateBarcode(new Barcode("garage-01", BarcodeSymbology.Code128));
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
            Mock.Of<IBackgroundTaskObserver>(),
            Mock.Of<IBarcodeAssignmentService>(),
            Mock.Of<IBarcodeScanSession>());

        var initialization = viewModel.InitializeAsync(container.ContainerId.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(initialization.IsCompleted, Is.False);
            Assert.That(viewModel.Name, Is.EqualTo("Garage"));
            Assert.That(viewModel.BarcodeValue, Is.EqualTo("garage-01"));
            Assert.That(viewModel.BarcodeSymbology, Is.EqualTo("Code128"));
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

    [Test]
    public async Task SaveBarcodeCommand_WhenReplacementIsConfirmed_AssignsAndPublishesBarcode()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Top shelf");
        var details = new Mock<IContainerDetailsHandler>();
        details.Setup(handler => handler.GetSummaryAsync(container.ContainerId.ToString()))
            .ReturnsAsync(new ContainerDetailsSummary(container, 0, 0));
        var assignments = new Mock<IBarcodeAssignmentService>();
        assignments.Setup(service => service.UpdateContainerAsync(container, It.IsAny<Barcode>()))
            .Callback<Container, Barcode?>((target, barcode) => target.UpdateBarcode(barcode));
        var popup = new Mock<IPopupService>();
        popup.Setup(service => service.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>())).ReturnsAsync(true);
        var queries = new Mock<IContainerDetailsQueryHandler>();
        queries.Setup(handler => handler.QueryItemsAsync(container.ContainerId.ToString(), null, 0, 5))
            .ReturnsAsync([]);
        var itemCoordinator = new ContainerDetailsItemsCoordinator(
            details.Object,
            queries.Object,
            Mock.Of<IImagePathResolver>(),
            Mock.Of<INavigationService>(),
            popup.Object,
            new PopupDefinitionService(),
            new ItemConsumptionCoordinator(Mock.Of<IItemDetailsQueryHandler>(), Mock.Of<IItemInventoryCommandService>(), popup.Object, new PopupDefinitionService()),
            Mock.Of<IBackgroundTaskObserver>());
        var viewModel = new ContainerDetailsViewModel(
            Mock.Of<IDeleteContainerCommandHandler>(), Mock.Of<IUpdateContainerNotesCommandHandler>(),
            Mock.Of<IImagePathResolver>(), popup.Object, new PopupDefinitionService(), CreateImageService(),
            Mock.Of<INavigationService>(), Mock.Of<IApplicationSettings>(), Mock.Of<IPhotoBackgroundOperationTracker>(),
            itemCoordinator, Mock.Of<IBackgroundTaskObserver>(), assignments.Object, Mock.Of<IBarcodeScanSession>());
        await viewModel.InitializeAsync(container.ContainerId.ToString());
        viewModel.BarcodeValueDraft = "garage-01";
        viewModel.BarcodeSymbologyDraft = BarcodeSymbology.Code128;

        await viewModel.SaveBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.BarcodeValue, Is.EqualTo("garage-01"));
            Assert.That(viewModel.BarcodeSymbology, Is.EqualTo("Code128"));
            Assert.That(viewModel.IsEditingBarcode, Is.False);
        });
        assignments.Verify(service => service.UpdateContainerAsync(container,
            It.Is<Barcode>(barcode => barcode.Value == "garage-01" && barcode.Symbology == BarcodeSymbology.Code128)), Times.Once);
    }

    [Test]
    public async Task ScanBarcodeCommand_WhenContainerHasNoBarcode_AssignsAndPublishesScannedBarcode()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Top shelf");
        var details = new Mock<IContainerDetailsHandler>();
        details.Setup(handler => handler.GetSummaryAsync(container.ContainerId.ToString()))
            .ReturnsAsync(new ContainerDetailsSummary(container, 0, 0));
        var assignments = new Mock<IBarcodeAssignmentService>();
        assignments.Setup(service => service.UpdateContainerAsync(container, It.IsAny<Barcode>()))
            .Callback<Container, Barcode?>((target, barcode) => target.UpdateBarcode(barcode));
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("garage-01", BarcodeSymbology.QrCode));
        var queries = new Mock<IContainerDetailsQueryHandler>();
        queries.Setup(handler => handler.QueryItemsAsync(container.ContainerId.ToString(), null, 0, 5))
            .ReturnsAsync([]);
        var itemCoordinator = new ContainerDetailsItemsCoordinator(
            details.Object,
            queries.Object,
            Mock.Of<IImagePathResolver>(),
            Mock.Of<INavigationService>(),
            Mock.Of<IPopupService>(),
            new PopupDefinitionService(),
            new ItemConsumptionCoordinator(
                Mock.Of<IItemDetailsQueryHandler>(),
                Mock.Of<IItemInventoryCommandService>(),
                Mock.Of<IPopupService>(),
                new PopupDefinitionService()),
            Mock.Of<IBackgroundTaskObserver>());
        var viewModel = new ContainerDetailsViewModel(
            Mock.Of<IDeleteContainerCommandHandler>(), Mock.Of<IUpdateContainerNotesCommandHandler>(),
            Mock.Of<IImagePathResolver>(), Mock.Of<IPopupService>(), new PopupDefinitionService(), CreateImageService(),
            Mock.Of<INavigationService>(), Mock.Of<IApplicationSettings>(), Mock.Of<IPhotoBackgroundOperationTracker>(),
            itemCoordinator, Mock.Of<IBackgroundTaskObserver>(), assignments.Object, scanner.Object);
        await viewModel.InitializeAsync(container.ContainerId.ToString());
        viewModel.EditBarcodeCommand.Execute(null);

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.BarcodeValue, Is.EqualTo("garage-01"));
            Assert.That(viewModel.BarcodeSymbology, Is.EqualTo("QrCode"));
            Assert.That(viewModel.IsEditingBarcode, Is.False);
        });
        assignments.Verify(service => service.UpdateContainerAsync(container,
            It.Is<Barcode>(barcode => barcode.Value == "garage-01" && barcode.Symbology == BarcodeSymbology.QrCode)), Times.Once);
    }

    [Test]
    public async Task ScanBarcodeCommand_WhenBarcodeIsAlreadyAssigned_ShowsInUseErrorWithoutThrowing()
    {
        var container = new Container(Guid.NewGuid(), "Garage", "Top shelf");
        var details = new Mock<IContainerDetailsHandler>();
        details.Setup(handler => handler.GetSummaryAsync(container.ContainerId.ToString()))
            .ReturnsAsync(new ContainerDetailsSummary(container, 0, 0));
        var assignments = new Mock<IBarcodeAssignmentService>();
        assignments.Setup(service => service.UpdateContainerAsync(container, It.IsAny<Barcode>()))
            .ThrowsAsync(new BarcodeAlreadyAssignedException("garage-01", BarcodeOwnerKind.Container, "Archive box"));
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("garage-01", BarcodeSymbology.QrCode));
        var queries = new Mock<IContainerDetailsQueryHandler>();
        queries.Setup(handler => handler.QueryItemsAsync(container.ContainerId.ToString(), null, 0, 5))
            .ReturnsAsync([]);
        var popup = Mock.Of<IPopupService>();
        var itemCoordinator = new ContainerDetailsItemsCoordinator(
            details.Object,
            queries.Object,
            Mock.Of<IImagePathResolver>(),
            Mock.Of<INavigationService>(),
            popup,
            new PopupDefinitionService(),
            new ItemConsumptionCoordinator(
                Mock.Of<IItemDetailsQueryHandler>(),
                Mock.Of<IItemInventoryCommandService>(),
                popup,
                new PopupDefinitionService()),
            Mock.Of<IBackgroundTaskObserver>());
        var viewModel = new ContainerDetailsViewModel(
            Mock.Of<IDeleteContainerCommandHandler>(), Mock.Of<IUpdateContainerNotesCommandHandler>(),
            Mock.Of<IImagePathResolver>(), popup, new PopupDefinitionService(), CreateImageService(),
            Mock.Of<INavigationService>(), Mock.Of<IApplicationSettings>(), Mock.Of<IPhotoBackgroundOperationTracker>(),
            itemCoordinator, Mock.Of<IBackgroundTaskObserver>(), assignments.Object, scanner.Object);
        string? error = null;
        viewModel.ErrorOccurred += message => error = message;
        await viewModel.InitializeAsync(container.ContainerId.ToString());
        viewModel.EditBarcodeCommand.Execute(null);

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.EqualTo("This barcode is already in use."));
            Assert.That(viewModel.ErrorMessage, Is.EqualTo("This barcode is already in use."));
            Assert.That(viewModel.IsEditingBarcode, Is.True);
            Assert.That(viewModel.BarcodeValue, Is.Empty);
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
