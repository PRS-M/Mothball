using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.ValueObjects;
using Moq;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.UI.Features.Containers.AddContainer;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Containers;

[TestFixture]
public sealed class AddContainerViewModelTests
{
    [Test]
    public async Task ScanBarcodeCommand_WhenScanCompletes_PopulatesBarcodeFields()
    {
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync())
            .ReturnsAsync(new Barcode("box-01", BarcodeSymbology.Code128));
        var viewModel = CreateViewModel(Mock.Of<ICreateContainerCommandHandler>(), scanner.Object);

        await viewModel.ScanBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.BarcodeValue, Is.EqualTo("box-01"));
            Assert.That(viewModel.BarcodeSymbology, Is.EqualTo(BarcodeSymbology.Code128));
        });
    }

    [Test]
    public async Task SaveContainerCommand_WithBarcode_PassesBarcodeToCreateHandler()
    {
        var createContainer = new Mock<ICreateContainerCommandHandler>();
        createContainer.Setup(handler => handler.CreateAsync("Archive box", "Top shelf", null, It.IsAny<Barcode>()))
            .ReturnsAsync(new Container(Guid.NewGuid(), "Archive box", "Top shelf"));
        var viewModel = CreateViewModel(createContainer.Object);
        viewModel.Name = "Archive box";
        viewModel.Notes = "Top shelf";
        viewModel.BarcodeValue = "box-01";
        viewModel.BarcodeSymbology = BarcodeSymbology.Code128;

        await viewModel.SaveContainerCommand.ExecuteAsync(null);

        createContainer.Verify(handler => handler.CreateAsync(
            "Archive box",
            "Top shelf",
            null,
            It.Is<Barcode>(barcode => barcode.Value == "box-01" && barcode.Symbology == BarcodeSymbology.Code128)), Times.Once);
    }

    private static AddContainerViewModel CreateViewModel(
        ICreateContainerCommandHandler createContainer,
        IBarcodeScanSession? barcodeScanner = null)
        => new(
            new ImageService(
                Mock.Of<IPhotoSourceReader>(),
                Mock.Of<IPhotoFilePersistenceService>(),
                Mock.Of<ITemporaryPhotoService>(),
                Mock.Of<IPhotoDeletionService>(),
                Mock.Of<IInventoryCommandRepository>()),
            createContainer,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(),
            Mock.Of<IPopupService>(),
            Mock.Of<IPopupDefinitionService>(),
            barcodeScanner ?? Mock.Of<IBarcodeScanSession>());
}