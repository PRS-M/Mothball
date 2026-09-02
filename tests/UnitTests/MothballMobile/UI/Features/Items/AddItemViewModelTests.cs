using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
    public void SaveCommand_WhenCreateThrows_RecordsErrorAndRethrows()
    {
        var createItem = new Mock<ICreateItemCommandHandler>();
        createItem.Setup(handler => handler.CreateAsync("Widget", "", null, 1, null))
            .ThrowsAsync(new InvalidOperationException("disk full"));
        var viewModel = CreateViewModel(createItem.Object, isAdvancedMode: false);
        viewModel.Name = "Widget";

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await viewModel.SaveCommand.ExecuteAsync(null));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo("disk full"));
            Assert.That(viewModel.ErrorMessage, Is.EqualTo("disk full"));
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

    private static AddItemViewModel CreateViewModel(
        ICreateItemCommandHandler createItem,
        bool isAdvancedMode,
        IBarcodeScanSession? barcodeScanner = null)
        => new(
            new ImageService(
                Mock.Of<IPhotoSourceReader>(),
                Mock.Of<IPhotoFilePersistenceService>(),
                Mock.Of<ITemporaryPhotoService>(),
                Mock.Of<IPhotoDeletionService>(),
                Mock.Of<IInventoryCommandRepository>()),
            createItem,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(settings => settings.IsAdvancedMode == isAdvancedMode),
            NullLogger<AddItemViewModel>.Instance,
            Mock.Of<IPopupService>(),
            Mock.Of<IPopupDefinitionService>(),
            barcodeScanner ?? Mock.Of<IBarcodeScanSession>());
}