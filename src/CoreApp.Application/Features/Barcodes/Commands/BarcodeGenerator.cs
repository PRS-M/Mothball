using CoreApp.Application.Contracts;
using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Creates application-owned barcode values for inventory records and reservations.
/// </summary>
public static class BarcodeGenerator
{
    /// <summary>
    /// Creates a stable Code 128 SKU from an existing record or reservation identifier.
    /// </summary>
    /// <param name="recordId">The identifier of the inventory record or reservation.</param>
    /// <returns>A Code 128 barcode containing the stable internal SKU.</returns>
    public static Barcode CreateInternalSku(Guid recordId)
    {
        if (recordId == Guid.Empty)
        {
            throw new ArgumentException("Record ID cannot be empty.", nameof(recordId));
        }

        return new Barcode($"MB-{recordId:N}".ToUpperInvariant(), BarcodeSymbology.Code128);
    }

    /// <summary>
    /// Creates a generated barcode in the symbology selected by the user.
    /// </summary>
    /// <param name="recordId">The identifier of the inventory record.</param>
    /// <param name="ownerKind">The type of inventory record.</param>
    /// <param name="symbology">The requested barcode symbology.</param>
    /// <returns>A generated value encoded as either Code 128 or a versioned Mothball QR code.</returns>
    public static Barcode Create(Guid recordId, BarcodeOwnerKind ownerKind, BarcodeSymbology symbology)
    {
        if (recordId == Guid.Empty)
        {
            throw new ArgumentException("Record ID cannot be empty.", nameof(recordId));
        }

        return symbology switch
        {
            BarcodeSymbology.Code128 or BarcodeSymbology.QrCode => new Barcode(
                $"mothball://v1/{GetOwnerSegment(ownerKind)}/{recordId:N}",
                symbology),
            _ => throw new NotSupportedException($"Mothball does not generate {symbology} values."),
        };
    }

    private static string GetOwnerSegment(BarcodeOwnerKind ownerKind)
        => ownerKind switch
        {
            BarcodeOwnerKind.Item => "item",
            BarcodeOwnerKind.Container => "container",
            _ => throw new ArgumentOutOfRangeException(nameof(ownerKind), ownerKind, "Unsupported barcode owner kind."),
        };
}
