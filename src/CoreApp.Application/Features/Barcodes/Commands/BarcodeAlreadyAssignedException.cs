using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Indicates that a barcode cannot be assigned because another inventory record already owns it.
/// </summary>
public sealed class BarcodeAlreadyAssignedException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BarcodeAlreadyAssignedException"/> class.
    /// </summary>
    /// <param name="barcodeValue">The barcode value that is already assigned.</param>
    /// <param name="ownerKind">The type of record that owns the barcode.</param>
    /// <param name="ownerName">The name of the record that owns the barcode.</param>
    public BarcodeAlreadyAssignedException(string barcodeValue, BarcodeOwnerKind ownerKind, string ownerName)
        : base($"Barcode '{barcodeValue}' is already assigned to {ownerKind} '{ownerName}'.")
    {
    }
}