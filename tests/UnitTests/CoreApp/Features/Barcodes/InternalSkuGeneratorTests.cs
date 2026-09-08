using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Domain.ValueObjects;

namespace Mothball.Tests.Unit.Core.Features.Barcodes;

[TestFixture]
public sealed class InternalSkuGeneratorTests
{
    [Test]
    public void Create_UsesStableCode128SkuFormat()
    {
        var id = Guid.Parse("4f4d2d7e-6f3c-4c0b-9f3a-8b5c2f8c14a1");

        var result = InternalSkuGenerator.Create(id);

        Assert.That(result, Is.EqualTo(new Barcode("MB-4F4D2D7E6F3C4C0B9F3A8B5C2F8C14A1", BarcodeSymbology.Code128)));
    }

    [Test]
    public void Create_RejectsEmptyRecordId()
    {
        Assert.That(() => InternalSkuGenerator.Create(Guid.Empty), Throws.ArgumentException);
    }
}
