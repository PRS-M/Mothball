using CoreApp.Domain.Entities.Shared;

namespace MothballMobile.Infrastructure.BarcodeDocuments;

/// <summary>
/// Describes one inventory barcode label to include in a generated document.
/// </summary>
public sealed record BarcodeLabelData(
    string Name,
    string BarcodeValue,
    BarcodeSymbology Symbology);
