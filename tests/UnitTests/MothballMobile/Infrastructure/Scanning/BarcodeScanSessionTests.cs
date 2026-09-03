using CoreApp.Domain.ValueObjects;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Navigation;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.Infrastructure.Settings;
using MothballMobile.UI.Features.Scanning;

namespace Mothball.Tests.Unit.MothballMobile.Infrastructure.Scanning;

[TestFixture]
public sealed class BarcodeScanSessionTests
{
    [Test]
    public async Task ScanAsync_NavigatesToScannerAndReturnsCompletedBarcode()
    {
        var navigation = new Mock<INavigationService>();
        var session = new BarcodeScanSession(navigation.Object);
        var expected = new Barcode("1234567890123", BarcodeSymbology.Ean13);

        var scan = session.ScanAsync();
        await session.CompleteAsync(expected);

        Assert.That(await scan, Is.EqualTo(expected));
        navigation.Verify(service => service.GoToAsync(NavigationRoutes.BarcodeScanner), Times.Once);
        navigation.Verify(service => service.GoBackAsync(), Times.Once);
    }

    [Test]
    public async Task ScanAsync_ReturnsNullWhenCancelled()
    {
        var session = new BarcodeScanSession(Mock.Of<INavigationService>());
        var scan = session.ScanAsync();

        await session.CompleteAsync(null);

        Assert.That(await scan, Is.Null);
    }

    [Test]
    public async Task CompleteAsync_WaitsForScannerNavigationBeforeReturningBarcode()
    {
        var navigation = new Mock<INavigationService>();
        var scannerNavigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        navigation.Setup(service => service.GoBackAsync()).Returns(scannerNavigation.Task);
        var session = new BarcodeScanSession(navigation.Object);
        var expected = new Barcode("crate-17", BarcodeSymbology.Code128);

        var scan = session.ScanAsync();
        var completion = session.CompleteAsync(expected);

        Assert.That(scan.IsCompleted, Is.False);

        scannerNavigation.SetResult();
        await completion;

        Assert.That(await scan, Is.EqualTo(expected));
    }

    [TestCase(false, BarcodeSymbology.QrCode, true)]
    [TestCase(false, BarcodeSymbology.UpcE, false)]
    [TestCase(true, BarcodeSymbology.UpcE, true)]
    public void IsSymbologyAllowed_UsesBarcodeExtendedMode(
        bool isBarcodeExtendedMode,
        BarcodeSymbology symbology,
        bool expected)
    {
        var settings = Mock.Of<IApplicationSettings>(value =>
            value.IsBarcodeExtendedMode == isBarcodeExtendedMode);
        var viewModel = new BarcodeScannerViewModel(Mock.Of<IBarcodeScanSession>(), settings);

        Assert.That(viewModel.IsSymbologyAllowed(symbology), Is.EqualTo(expected));
    }
}