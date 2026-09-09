using CoreApp.Domain.ValueObjects;
using Moq;
using MothballMobile.Infrastructure.Scanning;
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
    public async Task CancelAsync_CompletesPendingScanWithoutNavigatingBack()
    {
        var navigation = new Mock<INavigationService>();
        var session = new BarcodeScanSession(navigation.Object);
        var scan = session.ScanAsync();

        await session.CancelAsync();

        Assert.That(await scan, Is.Null);
        navigation.Verify(service => service.GoBackAsync(), Times.Never);
    }

    [Test]
    public async Task CancelAsync_DoesNotReplaceBarcodeWhenCompletionIsNavigatingBack()
    {
        var navigation = new Mock<INavigationService>();
        var navigationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        navigation.Setup(service => service.GoBackAsync()).Returns(navigationCompleted.Task);
        var session = new BarcodeScanSession(navigation.Object);
        var expected = new Barcode("crate-17", BarcodeSymbology.Code128);

        var scan = session.ScanAsync();
        var completion = session.CompleteAsync(expected);
        await session.CancelAsync();

        navigationCompleted.SetResult();
        await completion;

        Assert.That(await scan, Is.EqualTo(expected));
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

    [TestCase(BarcodeSymbology.QrCode)]
    [TestCase(BarcodeSymbology.Code128)]
    [TestCase(BarcodeSymbology.Ean13)]
    [TestCase(BarcodeSymbology.Ean8)]
    [TestCase(BarcodeSymbology.UpcE)]
    public void IsSymbologyAllowed_AcceptsEveryRecognizedSymbology(BarcodeSymbology symbology)
    {
        var viewModel = new BarcodeScannerViewModel(Mock.Of<IBarcodeScanSession>());

        Assert.That(viewModel.IsSymbologyAllowed(symbology), Is.True);
    }
}
