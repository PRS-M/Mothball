namespace MothballMobile.Infrastructure.BarcodeDocuments;

/// <summary>
/// Defines the fixed A4 label grid used by barcode documents.
/// </summary>
public static class BarcodeDocumentLayout
{
    /// <summary>Number of labels placed on one page.</summary>
    public const int LabelsPerPage = 32;

    /// <summary>Width of an A4 page in PDF points.</summary>
    public const float PageWidth = 8.27f * 72f;

    /// <summary>Height of an A4 page in PDF points.</summary>
    public const float PageHeight = 11.69f * 72f;

    private const float PageMargin = 28f;
    private const float LabelGap = 10f;
    private const int Columns = 4;
    private const int Rows = 8;

    /// <summary>
    /// Gets the number of pages required for the supplied label count.
    /// </summary>
    public static int GetPageCount(int labelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(labelCount);
        return labelCount == 0 ? 0 : (int)Math.Ceiling(labelCount / (double)LabelsPerPage);
    }

    /// <summary>
    /// Gets the bounds of a label slot on an A4 page.
    /// </summary>
    /// <param name="slotIndex">Zero-based slot index within a page.</param>
    public static BarcodeLabelBounds GetBounds(int slotIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        if (slotIndex >= LabelsPerPage)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "A page contains 32 label slots.");
        }

        var labelWidth = (PageWidth - (2 * PageMargin) - ((Columns - 1) * LabelGap)) / Columns;
        var labelHeight = (PageHeight - (2 * PageMargin) - ((Rows - 1) * LabelGap)) / Rows;
        var row = slotIndex / Columns;
        var column = slotIndex % Columns;
        var x = PageMargin + column * (labelWidth + LabelGap);
        var y = PageMargin + row * (labelHeight + LabelGap);

        return new BarcodeLabelBounds(x, y, x + labelWidth, y + labelHeight);
    }
}

/// <summary>Represents a label slot in PDF points.</summary>
public readonly record struct BarcodeLabelBounds(float Left, float Top, float Right, float Bottom)
{
    /// <summary>Gets the slot width.</summary>
    public float Width => Right - Left;

    /// <summary>Gets the slot height.</summary>
    public float Height => Bottom - Top;
}
