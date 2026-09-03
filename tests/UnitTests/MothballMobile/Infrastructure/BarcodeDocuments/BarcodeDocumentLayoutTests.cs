using MothballMobile.Infrastructure.BarcodeDocuments;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.BarcodeDocuments;

[TestFixture]
public sealed class BarcodeDocumentLayoutTests
{
    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(32, 1)]
    [TestCase(33, 2)]
    [TestCase(64, 2)]
    public void GetPageCount_ReturnsPagesForLabelCount(int labelCount, int expectedPageCount)
    {
        Assert.That(BarcodeDocumentLayout.GetPageCount(labelCount), Is.EqualTo(expectedPageCount));
    }

    [Test]
    public void GetBounds_CreatesFourColumnsAndEightRowsWithinPage()
    {
        var bounds = Enumerable.Range(0, BarcodeDocumentLayout.LabelsPerPage)
            .Select(BarcodeDocumentLayout.GetBounds)
            .ToArray();

        Assert.That(bounds, Has.All.Matches<BarcodeLabelBounds>(value =>
            value.Left >= 0
            && value.Top >= 0
            && value.Right <= BarcodeDocumentLayout.PageWidth
            && value.Bottom <= BarcodeDocumentLayout.PageHeight
            && value.Width > 0
            && value.Height > 0));
        Assert.That(bounds[0].Left, Is.EqualTo(bounds[4].Left));
        Assert.That(bounds[0].Top, Is.EqualTo(bounds[1].Top));
        Assert.That(bounds[0].Right, Is.LessThan(bounds[1].Left));
        Assert.That(bounds[0].Bottom, Is.LessThan(bounds[8].Top));
    }

    [TestCase(-1)]
    [TestCase(32)]
    public void GetBounds_WhenSlotIsOutsidePage_Throws(int slotIndex)
    {
        Assert.That(() => BarcodeDocumentLayout.GetBounds(slotIndex), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
