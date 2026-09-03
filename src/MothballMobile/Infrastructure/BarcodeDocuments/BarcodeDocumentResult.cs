namespace MothballMobile.Infrastructure.BarcodeDocuments;

/// <summary>
/// Identifies a generated barcode PDF stored in local application data.
/// </summary>
public sealed record BarcodeDocumentResult(
    string FileName,
    string FullPath,
    int LabelCount,
    int PageCount);
