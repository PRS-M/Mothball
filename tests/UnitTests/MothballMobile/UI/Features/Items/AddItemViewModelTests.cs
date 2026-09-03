using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Inventory.Allocation;
using CoreApp.Domain.Entities.Shared;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.UI.Features.Items.AddItem;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Items;

[TestFixture]
public sealed class AddItemViewModelTests
{
    [Test]
    public async Task SaveCommand_InAdvancedMode_CreatesStandaloneItemWithEnteredUnassignedQuantity()
    {
        var createItem = new Mock<ICreateItemCommandHandler>();
        createItem.Setup(handler => handler.CreateAsync("Widget", "", null, 4, null))
            .ReturnsAsync(new CoreApp.Domain.Entities.ItemAggregate.Item("Widget", ""));
        var viewModel = CreateViewModel(createItem.Object, isAdvancedMode: true);
        viewModel.Name = "Widget";
        viewModel.Quantity = "4";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.That(viewModel.ShowQuantityField, Is.True);
        createItem.Verify(handler => handler.CreateAsync("Widget", "", null, 4, null), Times.Once);
    }

    [Test]
    public async Task SaveCommand_InSimpleMode_CreatesStandaloneItemWithDefaultQuantity()
    {
        var createItem = new Mock<ICreateItemCommandHandler>();
        createItem.Setup(handler => handler.CreateAsync("Widget", "", null, 1, null))
            .ReturnsAsync(new CoreApp.Domain.Entities.ItemAggregate.Item("Widget", ""));
        var viewModel = CreateViewModel(createItem.Object, isAdvancedMode: false);
        viewModel.Name = "Widget";
        viewModel.Quantity = "4";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.That(viewModel.ShowQuantityField, Is.False);
        createItem.Verify(handler => handler.CreateAsync("Widget", "", null, 1, null), Times.Once);
    }

    [Test]
    public async Task SaveCommand_WhenCreateThrows_RecordsGenericErrorWithoutRethrowing()
    {
        var createItem = new Mock<ICreateItemCommandHandler>();
        createItem.Setup(handler => handler.CreateAsync("Widget", "", null, 1, null))
            .ThrowsAsync(new InvalidOperationException("disk full"));
        var viewModel = CreateViewModel(createItem.Object, isAdvancedMode: false);
        viewModel.Name = "Widget";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ErrorMessage, Is.EqualTo("Something went wrong. Please try again."));
            Assert.That(viewModel.HasError, Is.True);
        });
    }

    [Test]
    public async Task ScanBarcodeCommand_WhenScanCompletes_PopulatesBarcodeFields()
    {
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("widget-01", BarcodeSymbology.Code128));
        var viewModel = CreateViewModel(Mock.Of<ICreateItemCommandHandler>(), false, scanner.Object);

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.BarcodeValue, Is.EqualTo("widget-01"));
            Assert.That(viewModel.BarcodeSymbology, Is.EqualTo(BarcodeSymbology.Code128));
        });
    }

    [Test]
    public void AvailableBarcodeSymbologies_WhenExtendedModeIsDisabled_ContainsOnlyQrCode()
    {
        var viewModel = CreateViewModel(Mock.Of<ICreateItemCommandHandler>(), false);

        Assert.That(viewModel.AvailableBarcodeSymbologies, Is.EquivalentTo(new[] { BarcodeSymbology.QrCode }));
    }

    [Test]
    public void AvailableBarcodeSymbologies_WhenExtendedModeIsEnabled_ContainsAllSupportedSymbologies()
    {
        var viewModel = CreateViewModel(
            Mock.Of<ICreateItemCommandHandler>(),
            false,
            isBarcodeExtendedMode: true);

        Assert.That(viewModel.AvailableBarcodeSymbologies, Is.EquivalentTo(Enum.GetValues<BarcodeSymbology>()));
    }

    [Test]
    public async Task ScanBarcodeCommand_WhileWaitingForScanner_ReportsBusy()
    {
        var pendingBarcode = new TaskCompletionSource<Barcode?>();
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync()).Returns(pendingBarcode.Task);
        var viewModel = CreateViewModel(Mock.Of<ICreateItemCommandHandler>(), false, scanner.Object);

        var scan = viewModel.ScanBarcodeCommand.ExecuteAsync(null);

        Assert.That(viewModel.IsBusy, Is.True);

        pendingBarcode.SetResult(null);
        await scan;

        Assert.That(viewModel.IsBusy, Is.False);
    }

    [Test]
    public async Task ScanBarcodeCommand_WhenBarcodeBelongsToItem_EntersReceiptMode()
    {
        var itemId = Guid.NewGuid();
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("widget-01", BarcodeSymbology.Code128));
        var inventoryQueries = new Mock<IInventoryQueryRepository>();
        inventoryQueries.Setup(repository => repository.FindBarcodeAsync("widget-01"))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Item, itemId, "Widget"));
        var viewModel = CreateViewModel(
            Mock.Of<ICreateItemCommandHandler>(),
            false,
            scanner.Object,
            inventoryQueries.Object);

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsReceivingExistingItem, Is.True);
            Assert.That(viewModel.IsItemMetadataEditable, Is.False);
            Assert.That(viewModel.ShowQuantityField, Is.True);
            Assert.That(viewModel.Name, Is.EqualTo("Widget"));
        });
    }

    [Test]
    public async Task ResolveBarcodeCommand_WhenBarcodeBelongsToItem_EntersReceiptMode()
    {
        var itemId = Guid.NewGuid();
        var inventoryQueries = new Mock<IInventoryQueryRepository>();
        inventoryQueries.Setup(repository => repository.FindBarcodeAsync("widget-01"))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Item, itemId, "Widget"));
        var viewModel = CreateViewModel(
            Mock.Of<ICreateItemCommandHandler>(),
            false,
            inventoryQueries: inventoryQueries.Object);
        viewModel.BarcodeValue = "  widget-01  ";

        await viewModel.ResolveBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.BarcodeValue, Is.EqualTo("widget-01"));
            Assert.That(viewModel.IsReceivingExistingItem, Is.True);
            Assert.That(viewModel.Name, Is.EqualTo("Widget"));
            Assert.That(viewModel.Quantity, Is.EqualTo("1"));
        });
    }

    [Test]
    public async Task ResolveBarcodeCommand_WhenBarcodeIsCleared_ExitsReceiptMode()
    {
        var itemId = Guid.NewGuid();
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("widget-01", BarcodeSymbology.Code128));
        var inventoryQueries = new Mock<IInventoryQueryRepository>();
        inventoryQueries.Setup(repository => repository.FindBarcodeAsync("widget-01"))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Item, itemId, "Widget"));
        var viewModel = CreateViewModel(
            Mock.Of<ICreateItemCommandHandler>(),
            false,
            scanner.Object,
            inventoryQueries.Object);

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);
        viewModel.BarcodeValue = string.Empty;
        await viewModel.ResolveBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsReceivingExistingItem, Is.False);
            Assert.That(viewModel.IsItemMetadataEditable, Is.True);
            Assert.That(viewModel.Name, Is.Empty);
        });
    }

    [Test]
    public async Task SaveCommand_InReceiptMode_ReceivesIntoSelectedContainer()
    {
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("widget-01", BarcodeSymbology.Code128));
        var inventoryQueries = new Mock<IInventoryQueryRepository>();
        inventoryQueries.Setup(repository => repository.FindBarcodeAsync("widget-01"))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Item, itemId, "Widget"));
        var receipts = new Mock<IItemReceiptService>();
        var viewModel = CreateViewModel(
            Mock.Of<ICreateItemCommandHandler>(),
            false,
            scanner.Object,
            inventoryQueries.Object,
            receipts.Object);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParams.ContainerId] = containerId.ToString() });
        viewModel.Quantity = "2";

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        receipts.Verify(service => service.ReceiveAsync(itemId, 2, containerId), Times.Once);
    }

    [Test]
    public async Task ScanDestinationContainerCommand_InReceiptMode_ReceivesIntoScannedContainer()
    {
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.SetupSequence(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("widget-01", BarcodeSymbology.Code128))
            .ReturnsAsync(new Barcode("box-01", BarcodeSymbology.Code128));
        var inventoryQueries = new Mock<IInventoryQueryRepository>();
        inventoryQueries.Setup(repository => repository.FindBarcodeAsync("widget-01"))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Item, itemId, "Widget"));
        inventoryQueries.Setup(repository => repository.FindBarcodeAsync("box-01"))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Container, containerId, "Box"));
        var receipts = new Mock<IItemReceiptService>();
        var viewModel = CreateViewModel(
            Mock.Of<ICreateItemCommandHandler>(),
            false,
            scanner.Object,
            inventoryQueries.Object,
            receipts.Object);

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);
        await viewModel.ScanDestinationContainerCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.That(viewModel.DestinationContainerName, Is.EqualTo("Box"));
        receipts.Verify(service => service.ReceiveAsync(itemId, 1, containerId), Times.Once);
    }

    private static AddItemViewModel CreateViewModel(
        ICreateItemCommandHandler createItem,
        bool isAdvancedMode,
        IBarcodeScanSession? barcodeScanner = null,
        IInventoryQueryRepository? inventoryQueries = null,
        IItemReceiptService? itemReceipts = null,
        bool isBarcodeExtendedMode = false)
        => new(
            new ImageService(
                Mock.Of<IPhotoSourceReader>(),
                Mock.Of<IPhotoFilePersistenceService>(),
                Mock.Of<ITemporaryPhotoService>(),
                Mock.Of<IPhotoDeletionService>(),
                Mock.Of<IInventoryCommandRepository>()),
            createItem,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(settings =>
                settings.IsAdvancedMode == isAdvancedMode
                && settings.IsBarcodeExtendedMode == isBarcodeExtendedMode),
            NullLogger<AddItemViewModel>.Instance,
            Mock.Of<IPopupService>(),
            Mock.Of<IPopupDefinitionService>(),
            barcodeScanner ?? Mock.Of<IBarcodeScanSession>(),
            inventoryQueries ?? Mock.Of<IInventoryQueryRepository>(),
            itemReceipts ?? Mock.Of<IItemReceiptService>());
}