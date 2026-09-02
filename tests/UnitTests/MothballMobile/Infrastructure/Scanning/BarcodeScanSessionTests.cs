using CoreApp.Domain.Entities.Shared;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Navigation;
using MothballMobile.Infrastructure.Scanning;

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
}