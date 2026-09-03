using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts;
using CoreApp.Domain.ValueObjects;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Navigation;
using MothballMobile.Infrastructure.Scanning;

namespace Mothball.Tests.Unit.MothballMobile.Infrastructure.Scanning;

[TestFixture]
public sealed class BarcodeLookupCoordinatorTests
{
    [Test]
    public async Task ScanAndNavigateAsync_WhenContainerOwnsBarcode_NavigatesToContainerDetails()
    {
        var barcode = new Barcode("crate-17", BarcodeSymbology.Code128);
        var ownerId = Guid.NewGuid();
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync()).ReturnsAsync(barcode);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(repository => repository.FindBarcodeAsync(barcode.Value))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Container, ownerId, "Archive crate"));
        var navigation = new Mock<INavigationService>();
        var coordinator = new BarcodeLookupCoordinator(scanner.Object, queries.Object, navigation.Object);

        var navigated = await coordinator.ScanAndNavigateAsync();

        Assert.That(navigated, Is.True);
        navigation.Verify(service => service.GoToAsync(
            NavigationRoutes.ContainerDetails,
            new ContainerDetailsNavigationRequest(ownerId)), Times.Once);
    }

    [Test]
    public async Task ScanAndNavigateAsync_WhenBarcodeHasNoOwner_DoesNotNavigate()
    {
        var barcode = new Barcode("unknown", BarcodeSymbology.QrCode);
        var scanner = new Mock<IBarcodeScanSession>();
        scanner.Setup(service => service.ScanAsync()).ReturnsAsync(barcode);
        var navigation = new Mock<INavigationService>();
        var coordinator = new BarcodeLookupCoordinator(
            scanner.Object,
            Mock.Of<IInventoryQueryRepository>(),
            navigation.Object);

        var navigated = await coordinator.ScanAndNavigateAsync();

        Assert.That(navigated, Is.False);
        navigation.VerifyNoOtherCalls();
    }
}