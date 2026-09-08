using CoreApp.Application.Contracts;
using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Creates application-owned barcode values in the symbology selected by the user.
/// </summary>
public static class GeneratedBarcodeGenerator
{
    /// <summary>
    /// Creates a generated barcode for an inventory record.
    /// </summary>
    /// <param name="recordId">The identifier of the inventory record.</param>
    /// <param name="ownerKind">The type of inventory record.</param>
    /// <param name="symbology">The requested barcode symbology.</param>
    /// <returns>A generated Code 128 SKU or versioned Mothball QR URI.</returns>
    public static Barcode Create(Guid recordId, BarcodeOwnerKind ownerKind, BarcodeSymbology symbology)
    {
        if (recordId == Guid.Empty)
        {
            throw new ArgumentException("Record ID cannot be empty.", nameof(recordId));
        }

        return symbology switch
        {
            BarcodeSymbology.Code128 => InternalSkuGenerator.Create(recordId),
            BarcodeSymbology.QrCode => new Barcode(
                $"mothball://v1/{(ownerKind == BarcodeOwnerKind.Item ? "item" : "container")}/{recordId:N}",
                BarcodeSymbology.QrCode),
            _ => throw new NotSupportedException($"Mothball does not generate {symbology} values."),
        };
    }
}
