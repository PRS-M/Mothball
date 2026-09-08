using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Domain.ValueObjects;

namespace Mothball.Tests.Unit.Core.Features.Barcodes;

[TestFixture]
public sealed class GeneratedBarcodeGeneratorTests
{
    private static readonly Guid RecordId = Guid.Parse("4f4d2d7e-6f3c-4c0b-9f3a-8b5c2f8c14a1");

    [Test]
    public void Create_Code128_ReturnsInternalSku()
    {
        var result = GeneratedBarcodeGenerator.Create(RecordId, BarcodeOwnerKind.Item, BarcodeSymbology.Code128);

        Assert.That(result, Is.EqualTo(new Barcode("MB-4F4D2D7E6F3C4C0B9F3A8B5C2F8C14A1", BarcodeSymbology.Code128)));
    }

    [Test]
    public void Create_QrCode_ReturnsVersionedRecordUri()
    {
        var result = GeneratedBarcodeGenerator.Create(RecordId, BarcodeOwnerKind.Container, BarcodeSymbology.QrCode);

        Assert.That(result, Is.EqualTo(new Barcode("mothball://v1/container/4f4d2d7e6f3c4c0b9f3a8b5c2f8c14a1", BarcodeSymbology.QrCode)));
    }

    [TestCase(BarcodeSymbology.Ean8)]
    [TestCase(BarcodeSymbology.Ean13)]
    public void Create_Ean_DoesNotGenerateCommercialCodes(BarcodeSymbology symbology)
    {
        Assert.That(
            () => GeneratedBarcodeGenerator.Create(RecordId, BarcodeOwnerKind.Item, symbology),
            Throws.TypeOf<NotSupportedException>());
    }
}
