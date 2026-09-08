using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Domain.ValueObjects;

namespace Mothball.Tests.Unit.Core.Features.Barcodes;

[TestFixture]
public sealed class BarcodeRegistryModelsTests
{
    [Test]
    public void Entry_CanRepresentReservedBarcodeWithoutOwner()
    {
        var barcode = new Barcode("MB-RESERVED", BarcodeSymbology.Code128);

        var entry = new BarcodeRegistryEntry(
            Guid.NewGuid(), barcode, BarcodeRegistryStatus.Reserved);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Barcode, Is.EqualTo(barcode));
            Assert.That(entry.Status, Is.EqualTo(BarcodeRegistryStatus.Reserved));
            Assert.That(entry.OwnerKind, Is.Null);
            Assert.That(entry.OwnerId, Is.Null);
        });
    }

    [Test]
    public void Entry_CanRepresentAssignedBarcode()
    {
        var ownerId = Guid.NewGuid();

        var entry = new BarcodeRegistryEntry(
            Guid.NewGuid(),
            new Barcode("MB-ASSIGNED", BarcodeSymbology.Code128),
            BarcodeRegistryStatus.Assigned,
            BarcodeOwnerKind.Item,
            ownerId,
            "Widget");

        Assert.Multiple(() =>
        {
            Assert.That(entry.OwnerKind, Is.EqualTo(BarcodeOwnerKind.Item));
            Assert.That(entry.OwnerId, Is.EqualTo(ownerId));
            Assert.That(entry.OwnerName, Is.EqualTo("Widget"));
        });
    }
}
