using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Creates stable, application-owned SKU barcodes for inventory records.
/// </summary>
public static class InternalSkuGenerator
{
    /// <summary>
    /// Creates a Code 128 SKU from an existing record identifier.
    /// </summary>
    /// <param name="recordId">The identifier of the inventory record.</param>
    /// <returns>A Code 128 barcode containing the stable internal SKU.</returns>
    public static Barcode Create(Guid recordId)
    {
        if (recordId == Guid.Empty)
        {
            throw new ArgumentException("Record ID cannot be empty.", nameof(recordId));
        }

        return new Barcode($"MB-{recordId:N}".ToUpperInvariant(), BarcodeSymbology.Code128);
    }
}
